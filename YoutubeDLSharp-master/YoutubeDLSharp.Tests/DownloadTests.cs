using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoutubeDLSharp.Options;

namespace YoutubeDLSharp.Tests
{
    [TestClass]
    public class DownloadTests
    {
        private const string URL = "https://www.youtube.com/watch?v=C0DPdy98e4c";
        private static YoutubeDL ydl;
        private static List<string> downloadedFiles;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            ydl = new YoutubeDL();
            ydl.YoutubeDLPath = "Lib\\youtube-dl.exe";
            ydl.FFmpegPath = "Lib\\ffmpeg.exe";
            downloadedFiles = new List<string>();
        }

        [TestMethod]
        public async Task TestVideoDownloadSimple()
        {
            var result = await ydl.RunVideoDownload(URL, mergeFormat: DownloadMergeFormat.Mkv);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(String.Empty, String.Join("", result.ErrorOutput));
            string file = result.Data;
            Assert.IsTrue(File.Exists(file));
            Assert.AreEqual(".mkv", Path.GetExtension(file));
            Assert.AreEqual("TEST VIDEO", Path.GetFileNameWithoutExtension(file));
            downloadedFiles.Add(file);
        }

        [TestMethod]
        public async Task TestAudioDownloadSimple()
        {
            var result = await ydl.RunAudioDownload(URL, AudioConversionFormat.Mp3);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(String.Empty, String.Join("", result.ErrorOutput));
            string file = result.Data;
            Assert.IsTrue(File.Exists(file));
            Assert.AreEqual(".mp3", Path.GetExtension(file));
            Assert.AreEqual("TEST VIDEO", Path.GetFileNameWithoutExtension(file));
            downloadedFiles.Add(file);
        }

        [TestMethod]
        public async Task TestVideoDownloadWithOptions()
        {
            ydl.OutputFolder = "Lib";
            ydl.OutputFileTemplate = "%(extractor)s_%(title)s_%(upload_date)s.%(ext)s";
            ydl.RestrictFilenames = true;
            var result = await ydl.RunVideoDownload(URL, format: "best", recodeFormat: VideoRecodeFormat.Mp4);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(String.Empty, String.Join("", result.ErrorOutput));
            string file = result.Data;
            Assert.IsTrue(File.Exists(file));
            Assert.AreEqual("Lib", Path.GetDirectoryName(file));
            Assert.AreEqual(".mp4", Path.GetExtension(file));
            Assert.AreEqual("youtube_TEST_VIDEO_20070221", Path.GetFileNameWithoutExtension(file));
            downloadedFiles.Add(file);
        }

        [TestMethod]
        public async Task TestVideoDownloadWithOverrideOptions()
        {
            var overrideOptions = new OptionSet()
            {
                Output = "%(extractor)s_%(title)s_%(upload_date)s.%(ext)s",
                RestrictFilenames = true,
                RecodeVideo = VideoRecodeFormat.Mp4
            };
            var result = await ydl.RunVideoDownload(URL, format: "best", overrideOptions: overrideOptions);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(String.Empty, String.Join("", result.ErrorOutput));
            string file = result.Data;
            Assert.IsTrue(File.Exists(file));
            Assert.AreEqual(".mp4", Path.GetExtension(file));
            Assert.AreEqual("youtube_TEST_VIDEO_20070221", Path.GetFileNameWithoutExtension(file));
            downloadedFiles.Add(file);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            foreach (var file in downloadedFiles)
            {
                File.Delete(file);
            }
        }
    }
}
