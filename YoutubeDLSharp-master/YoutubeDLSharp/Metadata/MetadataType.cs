
using System.Runtime.Serialization;

namespace YoutubeDLSharp.Metadata
{
    /// <summary>
    /// Possible types of media fetched by youtube-dl.
    /// </summary>
    public enum MetadataType
    {
        [EnumMember(Value = "video")]
        Video,
        [EnumMember(Value = "playlist")]
        Playlist,
        [EnumMember(Value = "multi_video")]
        MultiVideo,
        [EnumMember(Value = "url")]
        Url,
        [EnumMember(Value = "url_transparent")]
        UrlTransparent
    }
}
