using Newtonsoft.Json;

namespace EverywordDE.Models
{
    internal class Word
    {
        [JsonProperty("wordText")]
        internal string? WordText {get; set;} = string.Empty;
        [JsonProperty("ipa")]
        internal string? Ipa {get; set;} = string.Empty;
        [JsonProperty("meaning")]
        internal string? Meaning {get; set;} = string.Empty;
        [JsonProperty("etymology")]
        internal string? Etymology {get; set;} = string.Empty;
        [JsonProperty("synonyms")]
        internal List<string> Synonyms {get; set;} = new List<string>();
    }
}