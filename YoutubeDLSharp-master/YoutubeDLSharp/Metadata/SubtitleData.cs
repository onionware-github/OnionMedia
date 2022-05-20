using Newtonsoft.Json;

namespace YoutubeDLSharp.Metadata
{
    public class SubtitleData
    {
        [JsonProperty("ext")]
        public string Ext { get; set; }
        [JsonProperty("data")]
        public string Data { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
