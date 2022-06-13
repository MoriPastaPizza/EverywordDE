using EverywordDE.Models;
using Newtonsoft.Json;
using Serilog;

namespace EverywordDE.Helper
{
	internal class CredsManager
	{
		internal static TwitterCredentials TwitterCredentials => GetCreds();
		private static TwitterCredentials? _twitterCreds {get; set;}
		private static void LoadInTwitterCreds()
		{
			try
			{
				Log.Information("Loading twitter creds....");
				
				var file = File.ReadAllText("twitterCreds.json");
				_twitterCreds = JsonConvert.DeserializeObject<TwitterCredentials>(file);
				
				Log.Information("Loaded twitter creds!");
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, nameof(LoadInTwitterCreds));
				throw;
			}
		}
		
		private static TwitterCredentials GetCreds()
		{
			if (_twitterCreds == null)
			{
				LoadInTwitterCreds();
			}
			
			return _twitterCreds ?? throw new Exception("Twitter creds. could not be loaded!");
		}
	}
}