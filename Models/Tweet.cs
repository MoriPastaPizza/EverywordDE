using Newtonsoft.Json;

namespace EverywordDE.Models
{
    internal class Tweet{
        
        [JsonProperty("text")]
        internal string Text {get; set;} = string.Empty;
    }
}