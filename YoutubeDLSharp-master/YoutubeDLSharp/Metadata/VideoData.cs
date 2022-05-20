using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace YoutubeDLSharp.Metadata
{
    /// <summary>
    /// Represents the video metadata for one video as extracted by youtube-dl.
    /// </summary>
    public class VideoData
    {
        [JsonProperty("_type")]
        public MetadataType ResultType { get; set; }
        [JsonProperty("id")]
        public string ID { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("formats")]
        public FormatData[] Formats { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("ext")]
        public string Extension { get; set; }
        [JsonProperty("format")]
        public string Format { get; set; }
        [JsonProperty("player_url")]
        public string PlayerUrl { get; set; }
        [JsonProperty("extractor")]
        public string Extractor { get; set; }
        [JsonProperty("extractor_key")]
        public string ExtractorKey { get; set; }

        // If data refers to a playlist:
        [JsonProperty("entries")]
        public VideoData[] Entries { get; set; }
        // Additional optional fields:
        [JsonProperty("alt_title")]
        public string AltTitle { get; set; }
        [JsonProperty("display_id")]
        public string DisplayID { get; set; }
        [JsonProperty("thumbnails")]
        public ThumbnailData[] Thumbnails { get; set; }
        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("uploader")]
        public string Uploader { get; set; }
        [JsonProperty("license")]
        public string License { get; set; }
        [JsonProperty("creator")]
        public string Creator { get; set; }
        [JsonProperty("release_date")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? ReleaseDate { get; set; }
        [JsonProperty("timestamp")]
        public long? Timestamp { get; set; }
        [JsonProperty("upload_date")]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime? UploadDate { get; set; }
        [JsonProperty("uploader_id")]
        public string UploaderID { get; set; }
        [JsonProperty("uploader_url")]
        public string UploaderUrl { get; set; }
        [JsonProperty("channel")]
        public string Channel { get; set; }
        [JsonProperty("channel_id")]
        public string ChannelID { get; set; }
        [JsonProperty("channel_url")]
        public string ChannelUrl { get; set; }
        [JsonProperty("location")]
        public string Location { get; set; }
        [JsonProperty("subtitles")]
        public Dictionary<string, SubtitleData[]> Subtitles { get; set; }
        [JsonProperty("duration")]
        public float? Duration { get; set; }
        [JsonProperty("view_count")]
        public long? ViewCount { get; set; }
        [JsonProperty("like_count")]
        public long? LikeCount { get; set; }
        [JsonProperty("dislike_count")]
        public long? DislikeCount { get; set; }
        [JsonProperty("repost_count")]
        public long? RepostCount { get; set; }
        [JsonProperty("average_rating")]
        public double? AverageRating { get; set; }
        [JsonProperty("comment_count")]
        public long? CommentCount { get; set; }
        [JsonProperty("age_limit")]
        public int? AgeLimit { get; set; }
        [JsonProperty("webpage_url")]
        public string WebpageUrl { get; set; }
        [JsonProperty("categories")]
        public string[] Categories { get; set; }
        [JsonProperty("tags")]
        public string[] Tags { get; set; }
        [JsonProperty("is_live")]
        public bool? IsLive { get; set; }
        [JsonProperty("start_time")]
        public float? StartTime { get; set; }
        [JsonProperty("end_time")]
        public float? EndTime { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    class CustomDateTimeConverter : IsoDateTimeConverter
    {
        public CustomDateTimeConverter()
        {
            DateTimeFormat = "yyyyMMdd";
        }
    }
}
