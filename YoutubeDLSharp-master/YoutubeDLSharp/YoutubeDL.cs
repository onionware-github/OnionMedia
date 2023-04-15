using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using YoutubeDLSharp.Helpers;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace YoutubeDLSharp
{
	/// <summary>
	/// A class providing methods for downloading videos using youtube-dl.
	/// </summary>
	public class YoutubeDL
	{
		private static Regex rgxFile = new Regex("echo\\s\\\"?(.*)\\\"?", RegexOptions.Compiled);

		protected ProcessRunner runner;

		/// <summary>
		/// Path to the youtube-dl executable.
		/// </summary>
		public string YoutubeDLPath { get; set; } = "youtube-dl.exe";
		/// <summary>
		/// Path to the FFmpeg executable.
		/// </summary>
		public string FFmpegPath { get; set; } = "ffmpeg.exe";
		/// <summary>
		/// Path of the folder where items will be downloaded to.
		/// </summary>
		public string OutputFolder { get; set; } = Environment.CurrentDirectory;
		/// <summary>
		/// Template of the name of the downloaded file on youtube-dl style.
		/// See https://github.com/ytdl-org/youtube-dl#output-template.
		/// </summary>
		public string OutputFileTemplate { get; set; } = "%(title)s.%(ext)s";
		/// <summary>
		/// If set to true, file names a re restricted to ASCII characters.
		/// </summary>
		public bool RestrictFilenames { get; set; } = false;

		/* TODO IMPORTANT This flag does not work fully as expected:
         * Does not guarantee overwrites (see https://github.com/ytdl-org/youtube-dl/pull/20405)
         * Always overwrites post-processed files
         * (see https://github.com/ytdl-org/youtube-dl/issues/5173, https://github.com/ytdl-org/youtube-dl/issues/333)
         */
		public bool OverwriteFiles { get; set; } = true;

		/// <summary>
		/// If set to true, download errors are ignored and downloading is continued.
		/// </summary>
		public bool IgnoreDownloadErrors { get; set; } = true;

		/// <summary>
		/// Gets the product version of the youtube-dl executable file.
		/// </summary>
		public string Version
			=> FileVersionInfo.GetVersionInfo(Utils.GetFullPath(YoutubeDLPath)).FileVersion;

		/// <summary>
		/// Creates a new instance of the YoutubeDL class.
		/// </summary>
		/// <param name="maxNumberOfProcesses">The maximum number of concurrent youtube-dl processes.</param>
		public YoutubeDL(byte maxNumberOfProcesses = 4)
		{
			runner = new ProcessRunner(maxNumberOfProcesses);
		}

		/// <summary>
		/// Sets the maximal number of parallel download processes.
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		public async Task SetMaxNumberOfProcesses(byte count) => await runner.SetTotalCount(count);

		#region Process methods

		/// <summary>
		/// Runs youtube-dl with the given option set.
		/// </summary>
		/// <param name="urls">The video URLs passed to youtube-dl.</param>
		/// <param name="options">The OptionSet of youtube-dl options.</param>
		/// <param name="ct">A CancellationToken used to cancel the process.</param>
		/// <returns>A RunResult object containing the output of youtube-dl as an array of string.</returns>
		public async Task<RunResult<string[]>> RunWithOptions(string[] urls, OptionSet options, CancellationToken ct)
		{
			var output = new List<string>();
			var process = new YoutubeDLProcess(YoutubeDLPath);
			process.OutputReceived += (o, e) => output.Add(e.Data);
			(int code, string[] errors) = await runner.RunThrottled(process, urls, options, ct);
			return new RunResult<string[]>(code == 0, errors, output.ToArray());
		}

		/// <summary>
		/// Runs an update of youtube-dl.
		/// </summary>
		/// <returns>The output of youtube-dl as string.</returns>
		public async Task<string> RunUpdate()
		{
			string output = String.Empty;
			var process = new YoutubeDLProcess(YoutubeDLPath);
			process.OutputReceived += (o, e) => output = e.Data;
			await process.RunAsync(null, new OptionSet() { Update = true });
			return output;
		}

		/// <summary>
		/// Runs a fetch of information for the given video without downloading the video.
		/// </summary>
		/// <param name="url">The URL of the video to fetch information for.</param>
		/// <param name="ct">A CancellationToken used to cancel the process.</param>
		/// <param name="flat">If set to true, does not extract information for each video in a playlist.</param>
		/// <param name="overrideOptions">Override options of the default option set for this run.</param>
		/// <returns>A RunResult object containing a VideoData object with the requested video information.</returns>
		public async Task<RunResult<VideoData>> RunVideoDataFetch(string url,
			CancellationToken ct = default, bool flat = true, OptionSet overrideOptions = null)
		{
			var opts = GetDownloadOptions();
			if (overrideOptions != null)
			{
				opts = opts.OverrideOptions(overrideOptions);
			}
			opts.DumpSingleJson = true;
			opts.FlatPlaylist = flat;
			VideoData videoData = null;
			var process = new YoutubeDLProcess(YoutubeDLPath);
			process.OutputReceived += (o, e) =>
			{
				Debug.WriteLine($"---DATA START---\n{e.Data}\n---DATA END---");
				videoData = JsonConvert.DeserializeObject<VideoData>(e.Data);
				videoData = ApplyMissingDataFromEntries(videoData);
			};
			(int code, string[] errors) = await runner.RunThrottled(process, new[] { url }, opts, ct);
			return new RunResult<VideoData>(code == 0, errors, videoData);
		}

		static VideoData ApplyMissingDataFromEntries(VideoData videoData)
		{
			if (videoData is null) return videoData;

			if (videoData.Duration is null)
			{
				//Get the first entry with a duration
				var entry = videoData.Entries?.FirstOrDefault(vdE => vdE.Duration != null);
				videoData.Duration = entry?.Duration;
			}

			if (videoData.Thumbnail is null)
			{
				//Get the first entry with a thumbnail url
				var entry = videoData.Entries?.FirstOrDefault(vdE => !string.IsNullOrEmpty(vdE.Thumbnail));
				videoData.Thumbnail = entry?.Thumbnail;
			}

			if (videoData.UploadDate is null)
			{
				//Get the first entry with an uploaddate
				var entry = videoData.Entries?.FirstOrDefault(vdE => vdE.UploadDate != null);
				videoData.UploadDate = entry?.UploadDate;
			}
			return videoData;
		}

		/// <summary>
		/// Runs a download of the specified video with an optional conversion afterwards.
		/// </summary>
		/// <param name="url">The URL of the video to be downloaded.</param>
		/// <param name="format">A format selection string in youtube-dl style.</param>
		/// <param name="mergeFormat">If a merge is required, the container format of the merged downloads.</param>
		/// <param name="recodeFormat">The video format the output will be recoded to after download.</param>
		/// <param name="ct">A CancellationToken used to cancel the download.</param>
		/// <param name="progress">A progress provider used to get download progress information.</param>
		/// <param name="output">A progress provider used to capture the standard output.</param>
		/// <param name="overrideOptions">Override options of the default option set for this run.</param>
		/// <returns>A RunResult object containing the path to the downloaded and converted video.</returns>
		public async Task<RunResult<string>> RunVideoDownload(string url,
			string format = "bestvideo+bestaudio/best",
			DownloadMergeFormat mergeFormat = DownloadMergeFormat.Unspecified,
			VideoRecodeFormat recodeFormat = VideoRecodeFormat.None,
			CancellationToken ct = default, IProgress<DownloadProgress> progress = null,
			IProgress<string> output = null, OptionSet overrideOptions = null)
		{
			var opts = GetDownloadOptions();
			if (overrideOptions != null)
			{
				opts = opts.OverrideOptions(overrideOptions);
			}
			opts.Format = format;
			opts.MergeOutputFormat = mergeFormat;
			opts.RecodeVideo = recodeFormat;
			string outputFile = String.Empty;
			var process = new YoutubeDLProcess(YoutubeDLPath);
			// Report the used ytdl args
			output?.Report($"Arguments: {process.ConvertToArgs(new[] { url }, opts)}\n");
			process.OutputReceived += (o, e) =>
			{
				var match = rgxFile.Match(e.Data);
				if (match.Success)
				{
					outputFile = match.Groups[1].ToString().Trim('"');
					progress?.Report(new DownloadProgress(DownloadState.Success, data: outputFile));
				}
				output?.Report(e.Data);
			};
			(int code, string[] errors) = await runner.RunThrottled(process, new[] { url }, opts, ct, progress);
			return new RunResult<string>(code == 0, errors, outputFile);
		}

		/// <summary>
		/// Runs a download of the specified video playlist with an optional conversion afterwards.
		/// </summary>
		/// <param name="url">The URL of the playlist to be downloaded.</param>
		/// <param name="start">The index of the first playlist video to download (starting at 1).</param>
		/// <param name="end">The index of the last playlist video to dowload (if null, download to end).</param>
		/// <param name="items">An array of indices of playlist video to download.</param>
		/// <param name="format">A format selection string in youtube-dl style.</param>
		/// <param name="recodeFormat">The video format the output will be recoded to after download.</param>
		/// <param name="ct">A CancellationToken used to cancel the download.</param>
		/// <param name="progress">A progress provider used to get download progress information.</param>
		/// <param name="output">A progress provider used to capture the standard output.</param>
		/// <param name="overrideOptions">Override options of the default option set for this run.</param>
		/// <returns>A RunResult object containing the paths to the downloaded and converted videos.</returns>
		public async Task<RunResult<string[]>> RunVideoPlaylistDownload(string url,
			int? start = 1, int? end = null,
			int[] items = null,
			string format = "bestvideo+bestaudio/best",
			VideoRecodeFormat recodeFormat = VideoRecodeFormat.None,
			CancellationToken ct = default, IProgress<DownloadProgress> progress = null,
			IProgress<string> output = null, OptionSet overrideOptions = null)
		{
			var opts = GetDownloadOptions();
			if (overrideOptions != null)
			{
				opts = opts.OverrideOptions(overrideOptions);
			}
			opts.NoPlaylist = false;
			opts.PlaylistStart = start;
			opts.PlaylistEnd = end;
			if (items != null)
				opts.PlaylistItems = String.Join(",", items);
			opts.Format = format;
			opts.RecodeVideo = recodeFormat;
			var outputFiles = new List<string>();
			var process = new YoutubeDLProcess(YoutubeDLPath);
			// Report the used ytdl args
			output?.Report($"Arguments: {process.ConvertToArgs(new[] { url }, opts)}\n");
			process.OutputReceived += (o, e) =>
			{
				var match = rgxFile.Match(e.Data);
				if (match.Success)
				{
					var file = match.Groups[1].ToString().Trim('"');
					outputFiles.Add(file);
					progress?.Report(new DownloadProgress(DownloadState.Success, data: file));
				}
				output?.Report(e.Data);
			};
			(int code, string[] errors) = await runner.RunThrottled(process, new[] { url }, opts, ct, progress);
			return new RunResult<string[]>(code == 0, errors, outputFiles.ToArray());
		}

		/// <summary>
		/// Runs a download of the specified video with and converts it to an audio format afterwards.
		/// </summary>
		/// <param name="url">The URL of the video to be downloaded.</param>
		/// <param name="format">The audio format the video will be converted to after downloaded.</param>
		/// <param name="ct">A CancellationToken used to cancel the download.</param>
		/// <param name="progress">A progress provider used to get download progress information.</param>
		/// <param name="output">A progress provider used to capture the standard output.</param>
		/// <param name="overrideOptions">Override options of the default option set for this run.</param>
		/// <returns>A RunResult object containing the path to the downloaded and converted video.</returns>
		public async Task<RunResult<string>> RunAudioDownload(string url, AudioConversionFormat format,
			CancellationToken ct = default, IProgress<DownloadProgress> progress = null,
			IProgress<string> output = null, OptionSet overrideOptions = null)
		{
			var opts = GetDownloadOptions();
			if (overrideOptions != null)
			{
				opts = opts.OverrideOptions(overrideOptions);
			}
			opts.Format = "bestaudio/best";
			opts.ExtractAudio = true;
			opts.AudioFormat = format;
			string outputFile = String.Empty;
			var error = new List<string>();
			var process = new YoutubeDLProcess(YoutubeDLPath);
			// Report the used ytdl args
			output?.Report($"Arguments: {process.ConvertToArgs(new[] { url }, opts)}\n");
			process.OutputReceived += (o, e) =>
			{
				var match = rgxFile.Match(e.Data);
				if (match.Success)
				{
					outputFile = match.Groups[1].ToString().Trim('"');
					progress?.Report(new DownloadProgress(DownloadState.Success, data: outputFile));
				}
				output?.Report(e.Data);
			};
			(int code, string[] errors) = await runner.RunThrottled(process, new[] { url }, opts, ct, progress);
			return new RunResult<string>(code == 0, errors, outputFile);
		}

		/// <summary>
		/// Runs a download of the specified video playlist and converts all videos to an audio format afterwards.
		/// </summary>
		/// <param name="url">The URL of the playlist to be downloaded.</param>
		/// <param name="start">The index of the first playlist video to download (starting at 1).</param>
		/// <param name="end">The index of the last playlist video to dowload (if null, download to end).</param>
		/// <param name="items">An array of indices of playlist video to download.</param>
		/// <param name="format">The audio format the videos will be converted to after downloaded.</param>
		/// <param name="ct">A CancellationToken used to cancel the download.</param>
		/// <param name="progress">A progress provider used to get download progress information.</param>
		/// <param name="output">A progress provider used to capture the standard output.</param>
		/// <param name="overrideOptions">Override options of the default option set for this run.</param>
		/// <returns>A RunResult object containing the paths to the downloaded and converted videos.</returns>
		public async Task<RunResult<string[]>> RunAudioPlaylistDownload(string url,
			int? start = 1, int? end = null,
			int[] items = null, AudioConversionFormat format = AudioConversionFormat.Best,
			CancellationToken ct = default, IProgress<DownloadProgress> progress = null,
			IProgress<string> output = null, OptionSet overrideOptions = null)
		{
			var outputFiles = new List<string>();
			var opts = GetDownloadOptions();
			if (overrideOptions != null)
			{
				opts = opts.OverrideOptions(overrideOptions);
			}
			opts.NoPlaylist = false;
			opts.PlaylistStart = start;
			opts.PlaylistEnd = end;
			if (items != null)
				opts.PlaylistItems = String.Join(",", items);
			opts.Format = "bestaudio/best";
			opts.ExtractAudio = true;
			opts.AudioFormat = format;
			var process = new YoutubeDLProcess(YoutubeDLPath);
			// Report the used ytdl args
			output?.Report($"Arguments: {process.ConvertToArgs(new[] { url }, opts)}\n");
			process.OutputReceived += (o, e) =>
			{
				var match = rgxFile.Match(e.Data);
				if (match.Success)
				{
					var file = match.Groups[1].ToString().Trim('"');
					outputFiles.Add(file);
					progress?.Report(new DownloadProgress(DownloadState.Success, data: file));
				}
				output?.Report(e.Data);
			};
			(int code, string[] errors) = await runner.RunThrottled(process, new[] { url }, opts, ct, progress);
			return new RunResult<string[]>(code == 0, errors, outputFiles.ToArray());
		}

		/// <summary>
		/// Returns an option set with default options used for most downloading operations.
		/// </summary>
		protected virtual OptionSet GetDownloadOptions()
		{
			return new OptionSet()
			{
				IgnoreErrors = this.IgnoreDownloadErrors,
				IgnoreConfig = true,
				NoPlaylist = true,
				HlsPreferNative = true,
				ExternalDownloaderArgs = "-nostats -loglevel 0",
				Output = Path.Combine(OutputFolder, OutputFileTemplate),
				RestrictFilenames = this.RestrictFilenames,
				NoContinue = this.OverwriteFiles,
				NoOverwrites = !this.OverwriteFiles,
				NoPart = true,
				FfmpegLocation = Utils.GetFullPath(this.FFmpegPath),
				/* TODO This is used to retrieve the final file path.
                 * Could be replaced by https://github.com/ytdl-org/youtube-dl/pull/22769.
                 */
				Exec = "echo {}"
			};
		}

		#endregion
	}
}
