using Newtonsoft.Json;

namespace EverywordDE.Models
{
	internal class TwitterCredentials
	{
		[JsonProperty("apiToken")]
		internal string ApiToken {get; set;} = string.Empty;
		[JsonProperty("apiSecret")]
		internal string ApiSecret {get; set;} = string.Empty;
		[JsonProperty("token")]
		internal string Token {get; set;} = string.Empty;
		[JsonProperty("tokenSecret")]
		internal string TokenSecret {get; set;} = string.Empty;
	}
}