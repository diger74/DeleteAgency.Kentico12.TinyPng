using Newtonsoft.Json;

namespace Delete.Kentico12.TinyPng.Models
{
    public partial class TinyPngShrinkResponseInput
    {
        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public long? Size { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
    }
}