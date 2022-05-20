using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoutubeDLSharp.Options;

namespace YoutubeDLSharp.Tests
{
    [TestClass]
    public class OptionSetTests
    {
        [TestMethod]
        public void TestSimpleOptionFromString()
        {
            Option<string> stringOption = new Option<string>("-s");
            Option<bool> boolOption = new Option<bool>("--bool");
            Option<int> intOption = new Option<int>("--int", "-i");
            stringOption.SetFromString("-s someValue");
            Assert.AreEqual("someValue", stringOption.Value);
            boolOption.SetFromString("--bool");
            Assert.AreEqual(true, boolOption.Value);
            intOption.SetFromString("-i 42");
            Assert.AreEqual(42, intOption.Value);
        }

        [TestMethod]
        public void TestEnumOptionFromString()
        {
            Option<VideoRecodeFormat> videoOption = new Option<VideoRecodeFormat>("--vid");
            videoOption.SetFromString("--vid mp4");
            Assert.AreEqual(VideoRecodeFormat.Mp4, videoOption.Value);
        }

        [TestMethod]
        public void TestComplexOptionFromString()
        {
            Option<DateTime> dateOption = new Option<DateTime>("-d");
            dateOption.SetFromString("-d 20200322");
            Assert.AreEqual(new DateTime(2020, 03, 22), dateOption.Value);
        }

        [TestMethod]
        public void TestOptionSetFromString()
        {
            string[] lines = new[]
            {
                "-x",
                "# extract to mp3",
                "--audio-format mp3",
                "",
                "# Use this proxy",
                "--proxy 127.0.0.1:3128",
                "-o ~/Movies/%(title)s.%(ext)s",
                "--ffmpeg-location \"My Programs/ffmpeg.exe\""
            };
            OptionSet opts = OptionSet.FromString(lines);
            Assert.IsTrue(opts.ExtractAudio);
            Assert.AreEqual(AudioConversionFormat.Mp3, opts.AudioFormat);
            Assert.AreEqual("127.0.0.1:3128", opts.Proxy);
            Assert.AreEqual("~/Movies/%(title)s.%(ext)s", opts.Output);
            Assert.AreEqual("My Programs/ffmpeg.exe", opts.FfmpegLocation);
        }
        
        [TestMethod]
        public void TestCustomOptionSetFromString()
        {
            void AssertCustomOption(IOption option)
            {
                Assert.IsTrue(option.OptionStrings.Any());
                Assert.IsTrue(option.IsCustom);
                Assert.IsTrue(option.IsSet);
                Assert.IsNotNull(option.ToString());
            }

            const string firstOption = "--my-option";
            const string secondOption = "--my-valued-option";
            string[] lines = {
                firstOption,
                $"{secondOption} value"
            };
            
            // Assert custom options parsing from string
            OptionSet opts = OptionSet.FromString(lines);
            AssertCustomOption(opts.CustomOptions.First(s => s.DefaultOptionString == firstOption));
            AssertCustomOption(opts.CustomOptions.First(s => s.DefaultOptionString == secondOption));

            // Assert custom options cloning
            var cloned = opts.OverrideOptions(new OptionSet());
            AssertCustomOption(cloned.CustomOptions.First(s => s.DefaultOptionString == firstOption));
            AssertCustomOption(cloned.CustomOptions.First(s => s.DefaultOptionString == secondOption));
            
            // Assert custom options override
            var overrideOpts = opts.OverrideOptions(OptionSet.FromString(new[] { firstOption }));
            CollectionAssert.AllItemsAreUnique(overrideOpts.CustomOptions);
            
            // Assert custom options to string conversion
            Assert.IsFalse(string.IsNullOrWhiteSpace(opts.ToString()));
        }

        [TestMethod]
        public void TestCustomOptionSetByMethod()
        {
            // Can create custom options (also multiple with same name)
            OptionSet options = new OptionSet();
            options.AddCustomOption("--custom-string-option", "hello");
            options.AddCustomOption("--custom-bool-option", true);
            options.AddCustomOption("--custom-string-option", "world");
            Assert.AreEqual("--custom-string-option \"hello\" --custom-bool-option --custom-string-option \"world\"", options.ToString().Trim());

            // Can set values of custom options
            options.SetCustomOption("--custom-string-option", "new");
            Assert.AreEqual("--custom-string-option \"new\" --custom-bool-option --custom-string-option \"new\"", options.ToString().Trim());

            // Can delete custom options
            options.DeleteCustomOption("--custom-string-option");
            Assert.AreEqual("--custom-bool-option", options.ToString().Trim());
        }

        [TestMethod]
        public void TestOptionSetOverrideOptions()
        {
            var originalOptions = new OptionSet()
            {
                MergeOutputFormat = DownloadMergeFormat.Mp4,
                ExtractAudio = true,
                AudioFormat = AudioConversionFormat.Wav,
                AudioQuality = 0,
                Username = "bob",
                Verbose = true
            };
            var overrideOptions = new OptionSet()
            {
                MergeOutputFormat = DownloadMergeFormat.Mkv,
                Password = "passw0rd"
            };
            var newOptions = originalOptions.OverrideOptions(overrideOptions);
            Assert.AreEqual(AudioConversionFormat.Wav, newOptions.AudioFormat);
            Assert.AreEqual((byte)0, newOptions.AudioQuality);
            Assert.IsTrue(newOptions.Verbose);
            Assert.AreEqual(DownloadMergeFormat.Mkv, newOptions.MergeOutputFormat);
            Assert.AreEqual("bob", newOptions.Username);
            Assert.AreEqual("passw0rd", newOptions.Password);
        }
    }
}
