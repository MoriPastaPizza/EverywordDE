using System.Net;
using System.Text;
using EverywordDE.Helper;
using EverywordDE.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OAuth;
using Serilog;

namespace EverywordDE.Controller
{
	internal static class TwitterController
	{
		private static List<Word> MightyWordList {get; set;} = new List<Word>();
		private static int CurrentIndex {get; set;}
		private static readonly TimeSpan TweetTimeOut = TimeSpan.FromMinutes(4);

		private const string BaseAddress = "https://api.twitter.com";
		private const string PostTweetAddress = "/2/tweets";
		private const ushort MaxTweetChars = 280;
		private const int MaxTweetsToFetch = 5;
		
		// Don't forget to change this!
		private const long BotTwitterId = 1531722538442805249;
		private const string LastIndexFilePath = "LastIndex.txt";
		private const string RateLimitLimit = "x-rate-limit-limit";
		private const string RateLimitRemaining = "x-rate-limit-remaining";
		private const string RateLimitReset = "x-rate-limit-reset";


		internal static async Task Init()
		{
			try
			{
				Log.Information("Starting twitter bot...");
				
				InitWordList();
				CurrentIndex = await GetLatestWordIndex();
				var _ = Task.Factory.StartNew(async () => await TweetWordsTask(), TaskCreationOptions.LongRunning);
				
				Log.Information("Twitter bot started!");
			}
			catch(Exception ex)
			{
				Log.Fatal(ex, "Could not start twitter bot!");
			}
		}
		
		private static async Task TweetWordsTask()
		{
			try
			{
				CurrentIndex++;
				while(CurrentIndex < MightyWordList.Count)
				{
					var word = MightyWordList[CurrentIndex];
					var tweet = TweetBuilder(word);
					await SendTweet(tweet);
					await Task.Delay(TweetTimeOut);
					CurrentIndex++;
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, nameof(TweetWordsTask));
			}
		}
		
		private static Tweet TweetBuilder(Word word, int buildTry = 0)
		{
			var text = string.Empty;
			
			text += word.WordText;
			text += word.Ipa != string.Empty ? $" ({word.Ipa})" : string.Empty;
			text += word.Etymology != string.Empty ? $" {word.Etymology}" : string.Empty;
			text += word.Meaning != string.Empty ? $" {word.Meaning}" : string.Empty;
			
			if(word.Synonyms.Count > 0)
			{
				text += " Syn.: ";
				foreach(var syn in word.Synonyms)
				{
					text += syn + ", ";
				}
				text.Remove(text.Length - 1);
			}
						
			if(text.Length > MaxTweetChars)
			{
				buildTry++;
				switch (buildTry)
				{
					case 1:
						word.Ipa = string.Empty;
						return TweetBuilder(word, buildTry);
					case 2:
						word.Etymology = string.Empty;
						return TweetBuilder(word, buildTry);
					case 3:
						if(word.Synonyms.Count > 0)
						{
							word.Synonyms.RemoveAt(word.Synonyms.Count -1);
							return TweetBuilder(word, 2);
						}
						return TweetBuilder(word, buildTry);
					case 4:
						word.Meaning = string.Empty;
						return TweetBuilder(word, buildTry);
					default:
						throw new Exception("ayy yoo wtf!?");
				}

			}
		
			return new Tweet
			{
				Text = text
			};
		}

		private static async Task SendTweet(Tweet tweet)
		{
			
			Log.Information("Sending tweet: " + tweet.Text);
			
			var bodyJson = JsonConvert.SerializeObject(tweet);
			var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

			using var client = ClientBuilder("POST", PostTweetAddress);
			using var res = await client.PostAsync("", content);
			
			Log.Debug($"Response from twitter-API: code {res.StatusCode} message: {await res.Content.ReadAsStringAsync()}");
			
			if(res.StatusCode == HttpStatusCode.TooManyRequests)
			{
				Log.Warning("Twitter-API rate limit exceeded!");
				
				var reset = res.Headers.FirstOrDefault(m => m.Key == RateLimitReset).Value.ToList()[0];
				var resetOffset = DateTimeOffset.FromUnixTimeSeconds(long.Parse(reset));
				var resetTimespan = resetOffset - DateTime.UtcNow;
				
				Log.Information("Limit will be reset in: " + resetTimespan);
				
				await Task.Delay(resetTimespan + TimeSpan.FromMinutes(1));
				
				Log.Information("Retrying....");
				
				await SendTweet(tweet);
				return;
			}
			
			if(!res.IsSuccessStatusCode)
			{
				throw new Exception(await res.Content.ReadAsStringAsync());
			}
				
			var limitRemain = res.Headers.FirstOrDefault(m => m.Key == RateLimitRemaining).Value.ToList()[0];
			Log.Information("Requests left: " + limitRemain);
			
			Log.Information("Writing current index: " + CurrentIndex);
			File.WriteAllText(LastIndexFilePath, CurrentIndex.ToString());
		}
		
		private static async Task<int> GetLatestWordIndex()
		{
			try
			{
				Log.Information("Getting latest tweet....");
				
				var addr = $"/2/users/{BotTwitterId}/tweets?max_results={MaxTweetsToFetch}";
				using var client = ClientBuilder("GET", addr);
				using var res = await client.GetAsync("");
				var content = await res.Content.ReadAsStringAsync();
				
				Log.Debug($"Response from twitter-API: code {res.StatusCode} message: {content}");
				
				if(res.StatusCode == HttpStatusCode.TooManyRequests)
				{
					Log.Warning("Twitter-API rate limit exceeded!");
					
					var reset = res.Headers.FirstOrDefault(m => m.Key == RateLimitReset).Value.ToList()[0];
					var resetOffset = DateTimeOffset.FromUnixTimeSeconds(long.Parse(reset));
					var resetTimespan = resetOffset - DateTime.UtcNow;
					
					Log.Information("Limit will be reset in: " + resetTimespan);
					
					await Task.Delay(resetTimespan + TimeSpan.FromMinutes(1));
					
					Log.Information("Retrying....");
					
					return await GetLatestWordIndex();
				}
				
				if(!res.IsSuccessStatusCode)
				{
					throw new Exception(await res.Content.ReadAsStringAsync());
				}
					
				var limitRemain = res.Headers.FirstOrDefault(m => m.Key == RateLimitRemaining).Value.ToList()[0];
				Log.Information("Requests left: " + limitRemain);
				
				var parsed = JObject.Parse(content);
				
				int indexFromApi;
				if(parsed.SelectToken("meta.result_count")?.Value<int>() == 0)
				{
					indexFromApi = 0;
				}
				else
				{
					var latestTweet = parsed.SelectToken("data[0].text")?.Value<string>();
					
					if(latestTweet == null) throw new Exception("Latest Tweet could not be found...");
					
					Log.Information("Latest tweet is: " + latestTweet);
					
					var allTweets = new List<string>();
					foreach(var word in MightyWordList)
					{
						allTweets.Add(TweetBuilder(word).Text);
					}
					
					indexFromApi = allTweets.FindIndex(m => m.Equals(latestTweet));
				}
			
				Log.Information("Index from api: " + indexFromApi);
				var indexFromFile = int.Parse(File.ReadAllText(LastIndexFilePath));
				Log.Information("Index from file: " + indexFromFile);
				
				if(indexFromApi < 0) throw new Exception("Latest word index from API not found!");
				if(indexFromApi != indexFromFile)
				{
					throw new Exception("Indices do not match!");
				}
								
				return indexFromFile;
			}
			catch(Exception ex)
			{
				Log.Error(ex, nameof(GetLatestWordIndex));
				throw;
			}
		}
				
		private static HttpClient ClientBuilder(string method, string dest)
		{
			try
			{
				var fullAddress = BaseAddress + dest;
				var client = new HttpClient();
				var creds = CredsManager.TwitterCredentials;
				var oAuthClient = OAuthRequest.ForProtectedResource(method, creds.ApiToken, creds.ApiSecret, creds.Token, creds.TokenSecret);
				oAuthClient.RequestUrl = fullAddress;
				var auth = oAuthClient.GetAuthorizationHeader();

				client.BaseAddress = new Uri(fullAddress);
				client.DefaultRequestHeaders.Add("Authorization", auth);
				return client;
			}
			catch(Exception ex)
			{
				Log.Error(ex, nameof(ClientBuilder));
				throw;
			}
		}
		
		private static void InitWordList()
		{
			Log.Information("Reading word list....");
			
			var wordsString = File.ReadAllLines("TheMightyList.txt");
			var allWords = new List<Word>();
			
			foreach(var wordString in wordsString)
			{
				allWords.Add(JsonConvert.DeserializeObject<Word>(wordString)!);
			}
			
			MightyWordList = allWords;
			
			Log.Information($"Built word list with {MightyWordList.Count}");
		}
	}
}