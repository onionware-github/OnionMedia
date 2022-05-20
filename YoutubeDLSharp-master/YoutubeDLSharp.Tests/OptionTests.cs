using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoutubeDLSharp.Options;

namespace YoutubeDLSharp.Tests
{
    [TestClass]
    public class OptionTests
    {
        [TestMethod]
        public void TestBooleanOptions()
        {
            var expected = new[]
            {
                "--ignore-config", "--no-playlist", "--no-continue", "--quiet", "--embed-thumbnail"
            };
            var options = new OptionSet()
            {
                IgnoreConfig = true,
                NoPlaylist = true,
                NoContinue = true,
                Quiet = true,
                EmbedThumbnail = true
            };
            CollectionAssert.AreEquivalent(expected, options.GetOptionFlags().ToArray());
        }

        [TestMethod]
        public void TestEnumOptions()
        {
            var expected = new[]
            {
                "--extract-audio", "--audio-format \"mp3\""
            };
            var options = new OptionSet()
            {
                ExtractAudio = true,
                AudioFormat = AudioConversionFormat.Mp3
            };
            CollectionAssert.AreEquivalent(expected, options.GetOptionFlags().ToArray());
        }

        [TestMethod]
        public void TestStringOptions()
        {
            var expected = new[]
            {
                "--format \"mp4/bestvideo\"", "--playlist-start 10", "--playlist-end 100", "--min-views 1000"
            };
            var options = new OptionSet()
            {
                Format = "mp4/bestvideo",
                PlaylistStart = 10,
                PlaylistEnd = 100,
                MinViews = 1000
            };
            CollectionAssert.AreEquivalent(expected, options.GetOptionFlags().ToArray());
        }

        [TestMethod]
        public void TestDateTimeOptions()
        {
            var expected = new[]
            {
                "--dateafter 20190101", "--datebefore 20191020"
            };
            var options = new OptionSet()
            {
                DateAfter = new DateTime(2019, 01, 01),
                DateBefore = new DateTime(2019, 10, 20)
            };
        }
    }
}
