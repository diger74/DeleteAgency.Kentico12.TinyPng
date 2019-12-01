namespace DeleteAgency.Kentico12.TinyPng.Models
{
    using Newtonsoft.Json;

    public partial class TinyPngShrinkResponse
    {
        [JsonProperty("input", NullValueHandling = NullValueHandling.Ignore)]
        public TinyPngShrinkResponseInput Input { get; set; }

        [JsonProperty("output", NullValueHandling = NullValueHandling.Ignore)]
        public TinyPngShrinkResponseOutput Output { get; set; }

        [JsonIgnore]
        public int CompressionCount { get; set; }

        [JsonIgnore]
        public string Location { get; set; }
    }
}