using System.IO;
using OnionMedia.Core.Models;
using OnionMedia.Core.Services;

namespace OnionMedia.Services;

sealed class LibraryInfoProvider : IDataCollectionProvider<LibraryInfo>
{
    private readonly IPathProvider pathProvider;
    public LibraryInfoProvider(IPathProvider pathProvider)
    {
        this.pathProvider = pathProvider;
    }
    
    public LibraryInfo[] GetItems()
    {
        return new[] 
        {
            new LibraryInfo("FFmpeg", "FFmpeg 64-bit static Windows build from www.gyan.dev", "GNU GPL v3", Path.Combine(pathProvider.InstallPath, "ExternalBinaries", "ffmpeg+yt-dlp", "FFmpeg_LICENSE"), "https://github.com/FFmpeg/FFmpeg/commit/9687cae2b4"),
            new LibraryInfo("yt-dlp", "yt-dlp", "Unlicense", Path.Combine(pathProvider.LicensesDir, "yt-dlp.txt"), "https://github.com/yt-dlp/yt-dlp"),
            new LibraryInfo("CommunityToolkit", "Microsoft", "MIT License", Path.Combine(pathProvider.LicensesDir, "communitytoolkit.txt"), "https://github.com/CommunityToolkit/WindowsCommunityToolkit"),
            new LibraryInfo("FFMpegCore", "Vlad Jerca", "MIT License", Path.Combine(pathProvider.LicensesDir, "FFMpegCore.txt"), "https://github.com/rosenbjerg/FFMpegCore"),
            new LibraryInfo("Newtonsoft.Json", "James Newton-King", "MIT License", Path.Combine(pathProvider.LicensesDir, "newtonsoft_json.txt"), "https://github.com/JamesNK/Newtonsoft.Json"),
            new LibraryInfo("PInvoke.User32", ".NET Foundation", "MIT License", Path.Combine(pathProvider.LicensesDir, "pinvoke_user32.txt"), "https://github.com/dotnet/pinvoke"),
            new LibraryInfo("TagLib#", "mono", "LGPL v2.1", Path.Combine(pathProvider.LicensesDir, "TagLibSharp.txt"), "https://github.com/mono/taglib-sharp"),
            new LibraryInfo("xFFmpeg.NET", "Tobias Haimerl(cmxl)", "MIT License", Path.Combine(pathProvider.LicensesDir, "xFFmpeg.NET.txt"), "https://github.com/cmxl/FFmpeg.NET"),
            new LibraryInfo("XamlBehaviors", "Microsoft", "MIT License", Path.Combine(pathProvider.LicensesDir, "microsoft_mit_license.txt"), "https://github.com/Microsoft/XamlBehaviors"),
            new LibraryInfo("YoutubeDLSharp", "Bluegrams", "BSD 3-Clause License", Path.Combine(pathProvider.LicensesDir, "YoutubeDLSharp.txt"), "https://github.com/Bluegrams/YoutubeDLSharp"),
            new LibraryInfo("YoutubeExplode", "Tyrrrz", "LGPL v3", Path.Combine(pathProvider.LicensesDir, "YoutubeExplode.txt"), "https://github.com/Tyrrrz/YoutubeExplode")
        };
    }
}