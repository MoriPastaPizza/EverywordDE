using EverywordDE.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace EverywordDE.Helper
{
	public class WikiParser
	{
		// This thing ain't pretty but she be doing her work
		// This was used once to parse the kaikki.org dict. just left it in if anyone is interested in this mess :)
		internal static List<Word> ParseWordList()
		{
			Log.Information("Parsing dict....");
			var fullString = File.ReadAllLines("kaikki.org-dictionary-German-words.json");
			var parsed = new List<Word>();

			foreach(var line in fullString){
				
				var jObj = JObject.Parse(line);
				var ipa = string.Empty;
				var meaning = string.Empty;
				var synonyms = new List<string>();
				
				if(jObj == null) return new List<Word>();
				
				var wordText = jObj.SelectToken("word")?.Value<string>() ?? string.Empty;
				var sounds = jObj.SelectToken("sounds")?.Value<JToken>();

				if(sounds != null && sounds.Count() > 0)
				{
					ipa = sounds.First<JToken>()?.SelectToken("ipa")?.Value<string>() ?? string.Empty;
					ipa = ipa.Replace("/", string.Empty);
				}
				
				var senses = jObj.SelectToken("senses");
				
				if(senses != null && senses.Count() > 0)
				{
					var rawGloss = senses.First!.SelectToken("raw_glosses");
				
					if(rawGloss != null && rawGloss.Count() > 0)
					{
						meaning = rawGloss.First?.Value<string>() ?? string.Empty;
					}
					
					var synonymsTokens = senses.First!.SelectToken("synonyms");
					
					if(synonymsTokens != null && synonymsTokens.Count() > 0)
					{
						foreach(var synonymsToken in synonymsTokens)
						{
							var innerToken = synonymsToken.SelectToken("word");
							synonyms.Add(innerToken?.Value<string>() ?? string.Empty);
						}
					}

				}
		
				var etymology = jObj?.SelectToken("etymology_text")?.Value<string>() ?? string.Empty;
		
				parsed.Add(new Word
				{
					WordText = wordText,
					Ipa = ipa ?? string.Empty,
					Meaning = meaning ?? string.Empty,
					Etymology = etymology,
					Synonyms = synonyms
				});
			}

			parsed.RemoveAll(x => 
				x!.WordText!.StartsWith('-') || 
				x.WordText.Length == 1);
			
			parsed = parsed.OrderBy(m => m.WordText).ToList();
			
			Log.Information($"Parsed {parsed.Count} Items!");
			
			return parsed;
		}
	}
}