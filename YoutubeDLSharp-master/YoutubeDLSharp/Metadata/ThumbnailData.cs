using Newtonsoft.Json;

namespace YoutubeDLSharp.Metadata
{
    public class ThumbnailData
    {
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("preference")]
        public int? Preference { get; set; }
        [JsonProperty("width")]
        public int? Width { get; set; }
        [JsonProperty("height")]
        public int? Height { get; set; }
        [JsonProperty("resolution")]
        public string Resolution { get; set; }
        [JsonProperty("filesize")]
        public int? Filesize { get; set; }
    }
}
