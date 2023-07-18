/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using FFmpeg.NET;
using FFmpeg.NET.Enums;
using OnionMedia.Core.Extensions;
using OnionMedia.Core.Models;
using System.Security;
using System.Net.Http;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeDLSharp.Metadata;
using Newtonsoft.Json;
using System.Text;
using System.Drawing;
using FFMpegCore;
using OnionMedia.Core.Services;
using YoutubeExplode.Videos;
using OnionMedia.Core.ViewModels.Dialogs;

namespace OnionMedia.Core.Classes
{
	public static class DownloaderMethods
	{
		private static readonly IDispatcherService dispatcher = IoC.Default.GetService<IDispatcherService>() ?? throw new ArgumentNullException();
		private static readonly IPathProvider pathProvider = IoC.Default.GetService<IPathProvider>() ?? throw new ArgumentNullException();

		public static readonly YoutubeClient youtube = new();

		public static readonly YoutubeDL downloadClient = new(5)
		{
			FFmpegPath = pathProvider.FFmpegPath,
			YoutubeDLPath = pathProvider.YtDlPath,
			OutputFolder = pathProvider.DownloaderTempdir,
			OverwriteFiles = true
		};

		private static string GetHardwareEncodingParameters(FormatData formatData) => AppSettings.Instance.HardwareEncoder switch
		{
			HardwareEncoder.Nvidia_NVENC => $"-vcodec h264_nvenc {(formatData.VideoBitrate > 0 ? $"-b:v {formatData.VideoBitrate.ToString().Replace(',', '.') + 'k'}" : "-profile:v high -q:v 20 -preset:v slower")}",
			HardwareEncoder.Intel_QSV => $"-vcodec h264_qsv {(formatData.VideoBitrate > 0 ? $"-b:v {formatData.VideoBitrate.ToString().Replace(',', '.') + 'k'}" : "-profile:v high -q:v 20 -preset:v slower")}",
			HardwareEncoder.AMD_AMF => $"-vcodec h264_amf {(formatData.VideoBitrate > 0 ? $"-b:v {formatData.VideoBitrate.ToString().Replace(',', '.') + 'k'}" : "-profile:v high -q:v 20 -preset:v slower")}",
			_ => string.Empty
		};

		public static async Task DownloadStreamAsync(StreamItemModel stream, bool getMP4, string customOutputDirectory = null)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			OptionSet ytOptions = new() { RestrictFilenames = true };

			//Creates a temp directory if it does not already exist.
			Directory.CreateDirectory(pathProvider.DownloaderTempdir);

			//Creates a new temp directory for this file
			string videotempdir = CreateVideoTempDir();


			SetProgressToDefault(stream);
			stream.CancelEventHandler += CancelDownload;

			var cToken = stream.CancelSource.Token;
			bool autoConvertToH264 = AppSettings.Instance.AutoConvertToH264AfterDownload;

			string tempfile = string.Empty;
			bool isShortened = false;
			Enums.ItemType itemType = Enums.ItemType.video;

			try
			{
				Debug.WriteLine($"Current State of Progress: {stream.ProgressInfo.Progress}");
				ytOptions.Output = Path.Combine(videotempdir, "%(id)s.%(ext)s");

				if (stream.CustomTimes)
				{
					ytOptions.AddCustomOption("--download-sections", $"*{stream.TimeSpanGroup.StartTime.WithoutMilliseconds()}.00-{stream.TimeSpanGroup.EndTime:hh\\:mm\\:ss}.00");
					ytOptions.AddCustomOption("--force-keyframes-at-cuts --compat", "no-direct-merge");
					ytOptions.ExternalDownloaderArgs = String.Empty;
					isShortened = true;
				}

				//Set download speed limit from AppSettings
				if (AppSettings.Instance.LimitDownloadSpeed && AppSettings.Instance.MaxDownloadSpeed > 0)
					ytOptions.LimitRate = (long)(AppSettings.Instance.MaxDownloadSpeed * 1000000 / 8);

				//Audiodownload
				if (!getMP4)
				{
					tempfile = await RunAudioDownloadAndConversionAsync(stream, ytOptions, isShortened, TimeSpan.Zero, cToken);
					itemType = Enums.ItemType.audio;
				}
				//Videodownload
				else
				{
					tempfile = await RunVideoDownloadAndConversionAsync(stream, ytOptions, isShortened, autoConvertToH264, TimeSpan.Zero, cToken);
					itemType = Enums.ItemType.video;
				}
			}
			catch (Exception ex)
			{
				stream.Downloading = false;
				stream.Converting = false;
				switch (ex)
				{
					default:
						Debug.WriteLine(ex.Message);
						stream.DownloadState = Enums.DownloadState.IsFailed;
						stream.ProgressInfo.IsCancelledOrFailed = true;
						return;

					case TaskCanceledException:
						Debug.WriteLine("The download has cancelled.");
						stream.DownloadState = Enums.DownloadState.IsCancelled;
						stream.ProgressInfo.IsCancelledOrFailed = true;
						return;

					case HttpRequestException:
						Debug.WriteLine("No internet connection.");
						stream.DownloadState = Enums.DownloadState.IsFailed;
						stream.ProgressInfo.IsCancelledOrFailed = true;
						return;

					case SecurityException:
						Debug.WriteLine("No access to the temp-path.");
						stream.DownloadState = Enums.DownloadState.IsFailed;
						stream.ProgressInfo.IsCancelledOrFailed = true;
						return;

					case IOException:
						Debug.WriteLine("Failed to save video to the temp-path.");
						stream.DownloadState = Enums.DownloadState.IsFailed;
						stream.ProgressInfo.IsCancelledOrFailed = true;
						NotEnoughSpaceException.ThrowIfNotEnoughSpace((IOException)ex);
						throw;
				}
			}

			stream.Converting = false;
			Debug.WriteLine($"Current State of Progress: {stream.ProgressInfo.Progress}");

			try
			{
				//Writes the tags into the file
				//TODO: Fix file corruptions while saving tags
				if (Path.GetExtension(tempfile) is ".mp3" or ".mp4" or ".m4a" or ".flac")
					SaveTags(tempfile, stream.Video);

				//Moves the video in the correct directory
				stream.Moving = true;
				stream.Path = await MoveToDisk(tempfile, stream.Video.Title, itemType, customOutputDirectory, cToken);

				stream.RaiseFinished();
				stream.DownloadState = Enums.DownloadState.IsDone;
			}
			catch (Exception ex)
			{
				stream.DownloadState = Enums.DownloadState.IsFailed;
				stream.ProgressInfo.IsCancelledOrFailed = true;
				switch (ex)
				{
					default:
						Debug.WriteLine(ex.Message);
						break;

					case OperationCanceledException:
						Debug.WriteLine("Moving operation get canceled.");
						stream.DownloadState = Enums.DownloadState.IsCancelled;
						break;

					case FileNotFoundException:
						Debug.WriteLine("File not found!");
						break;

					case UnauthorizedAccessException:
						Debug.WriteLine("No permissions to access the file.");
						throw;

					case PathTooLongException:
						Debug.WriteLine("Path is too long");
						throw;

					case DirectoryNotFoundException:
						Debug.WriteLine("Directory not found!");
						throw;

					case IOException:
						Debug.WriteLine("IOException occured.");
						NotEnoughSpaceException.ThrowIfNotEnoughSpace((IOException)ex);
						throw;
				}
			}
			finally { stream.Moving = false; }
		}

		private static void SetProgressToDefault(StreamItemModel stream)
		{
			stream.ProgressInfo.IsDone = false;
			stream.Converting = false;
			stream.DownloadState = Enums.DownloadState.IsLoading;
			stream.ConversionProgress = 0;
		}

		private static string CreateVideoTempDir()
		{
			string videotempdir;
			do videotempdir = Path.Combine(pathProvider.DownloaderTempdir, Path.GetRandomFileName());
			while (Directory.Exists(videotempdir));
			Directory.CreateDirectory(videotempdir);
			return videotempdir;
		}

		private static async Task<string> DownloadAudioAsync(StreamItemModel stream, OptionSet ytOptions, CancellationToken cToken = default)
		{
			if (AppSettings.Instance.DownloadsAudioFormat != AudioConversionFormat.Opus)
				ytOptions.AddCustomOption("-S", "ext");

			int downloadCounter = 0;
			do
			{
				downloadCounter++;
				if (downloadCounter > 1)
					stream.SetProgressToDefault();

				stream.Downloading = true;
				RunResult<string> result = await downloadClient.RunAudioDownload(stream.Video.Url, AudioConversionFormat.Best, cToken, stream.DownloadProgress, overrideOptions: ytOptions);
				stream.Downloading = false;

				//Return if the download completed sucessfully.
				if (!result.Data.IsNullOrEmpty())
					return result.Data;

				if (AppSettings.Instance.AutoRetryDownload)
					Debug.WriteLine("Download failed. Try: " + downloadCounter);
			}
			while (AppSettings.Instance.AutoRetryDownload && downloadCounter <= AppSettings.Instance.CountOfDownloadRetries);
			throw new Exception("Download failed!");
		}

		private static async Task<string> RunAudioDownloadAndConversionAsync(StreamItemModel stream, OptionSet ytOptions, bool isShortened = false, TimeSpan overhead = default, CancellationToken cToken = default)
		{
			//Download
			string tempfile = await DownloadAudioAsync(stream, ytOptions, cToken);

			string format = AppSettings.Instance.DownloadsAudioFormat.ToString().ToLower();
			if (AppSettings.Instance.DownloadsAudioFormat is AudioConversionFormat.Vorbis)
				format = "ogg";

			if (!tempfile.EndsWith(format))
			{
				stream.Converting = true;
				tempfile = await ConvertAudioAsync(tempfile, format, overhead, stream, cToken);
			}

			Debug.WriteLine(tempfile);
			stream.ProgressInfo.Progress = 100;
			return tempfile;
		}

		private static async Task<string> RunVideoDownloadAndConversionAsync(StreamItemModel stream, OptionSet ytOptions, bool isShortened = false, bool autoConvertToH264 = false, TimeSpan overhead = default, CancellationToken cToken = default)
		{
			DownloadMergeFormat videoMergeFormat = (autoConvertToH264 || isShortened) ? DownloadMergeFormat.Unspecified : DownloadMergeFormat.Mp4;
			VideoRecodeFormat videoRecodeFormat = (autoConvertToH264 || isShortened) ? VideoRecodeFormat.None : VideoRecodeFormat.Mp4;
			ytOptions.EmbedThumbnail = true;
			ytOptions.AddCustomOption("--format-sort", "hdr:SDR");

			Size originalSize = default;
			FormatData formatData = null;
			bool removeFormat = false;

			//Don't use DASH in trimmed videos
			if (stream.CustomTimes)
			{
				//formatData = GetBestFormatWithoutDash(stream, out originalSize);
				formatData = GetBestTrimmableFormat(stream);
			}
			else
			{
				//TODO: Check if that works on weaker PCs with less threads.
				//Download multiple fragments at the same time to increase speed.
				ytOptions.AddCustomOption("-N", 15);
			}

			string? formatString;
			if (formatData?.FormatId != null)
				formatString = $"{formatData.FormatId}{((formatData.AudioCodec.IsNullOrWhiteSpace() || formatData.AudioCodec == "none") && formatData.AudioBitrate is null or 0 && stream.Video.Formats.Any(f => f.VideoBitrate is null or 0 && f.AudioBitrate > 0) ? "+bestaudio[ext=m4a]" : string.Empty)}";
			else
				formatString = stream.QualityLabel.IsNullOrEmpty() ? "bestvideo+bestaudio/best" : stream.Format;

			if (removeFormat)
				formatString = null;

			int downloadCounter = 0;
			RunResult<string> result;
			do
			{
				downloadCounter++;
				if (downloadCounter > 1)
					stream.SetProgressToDefault();

				//Try to encode MP4 hardware-accelerated in the first try
				if (downloadCounter == 1 && formatData?.Extension == "mp4"
										 && AppSettings.Instance.AutoConvertToH264AfterDownload
										 && AppSettings.Instance.UseHardwareAcceleratedEncoding)
				{
					ytOptions.ExternalDownloaderArgs = GetHardwareEncodingParameters(formatData);
				}

				stream.Downloading = true;
				result = await downloadClient.RunVideoDownload(stream.Video.Url, formatString, videoMergeFormat, videoRecodeFormat, cToken, stream.DownloadProgress, overrideOptions: ytOptions, output: new Progress<string>(p => Debug.WriteLine(p)));
				stream.Downloading = false;

				if (!result.Data.IsNullOrEmpty())
					break;

				if (AppSettings.Instance.FallBackToSoftwareEncoding)
					ytOptions.ExternalDownloaderArgs = string.Empty;

				if (AppSettings.Instance.AutoRetryDownload)
					Debug.WriteLine("Download failed. Try: " + downloadCounter);
			}
			while (AppSettings.Instance.AutoRetryDownload && downloadCounter <= AppSettings.Instance.CountOfDownloadRetries);
			if (result.Data.IsNullOrEmpty())
				throw new Exception("Download failed!");


			string tempfile = result.Data;
			Debug.WriteLine(tempfile);
			stream.ProgressInfo.Progress = 100;

			//TODO Setting: Allow to recode ALWAYS to H264, even if it's already H264.
			var meta = await FFProbe.AnalyseAsync(tempfile);
			if ((autoConvertToH264 && meta.PrimaryVideoStream.CodecName != "h264") || Path.GetExtension(tempfile) != ".mp4")
			{
				stream.Converting = true;
				tempfile = await ConvertToMp4Async(tempfile, overhead, default, stream, originalSize, meta, cToken);
			}
			return tempfile;
		}

		private static FormatData GetBestTrimmableFormat(StreamItemModel stream)
		{
			bool isFromYoutube = stream.Video.Url.Contains("youtube.com") || stream.Video.Url.Contains("youtu.be");
			var validFormats = stream.FormatQualityLabels.Where(f => ((f.Key.Protocol != "http_dash_segments" && !isFromYoutube) || (isFromYoutube && !(f.Key.FormatNote ?? string.Empty).ToLower().Contains("dash video"))) && f.Key.Extension != "3gp" && f.Key.HDR == "SDR");
			var heightSortedFormats = validFormats.OrderByDescending(f => f.Key.Height);
			var extSortedFormats = heightSortedFormats.ThenBy(f => f.Key.Extension == "mp4" ? 0 : 1);
			var selectedFormat = stream.QualityLabel.IsNullOrEmpty()
				? extSortedFormats.FirstOrDefault().Key
				: extSortedFormats.FirstOrDefault(f => f.Key.Height <= stream.GetVideoHeight).Key;
			return selectedFormat;
		}

		private static FormatData GetBestFormatWithoutDash(StreamItemModel stream, out Size originalSize)
		{
			FormatData formatData = null;
			originalSize = default;

			//Get the formats that doesn't use DASH
			var formats = stream.FormatQualityLabels.Where(f => f.Key.Protocol != "http_dash_segments" && f.Key.Extension != "3gp" && f.Key.HDR == "SDR");
			formatData = formats.LastOrDefault(f => stream.QualityLabel.StartsWith(f.Value.ToString())).Key;

			//Return if a matching format was found.
			if (formatData != null) return formatData;

			//Search for the next higher format.
			int selectedHeight = int.Parse(stream.QualityLabel.TrimEnd('p'));
			var higherFormats = formats.Where(f => f.Value > selectedHeight);

			if (!higherFormats.Any()) return null;

			//Get the higher format that comes closest to the choosen format.
			//TODO: Downscale to the selected resolution? (Check for same aspect ratio)
			int min = higherFormats.Min(f => f.Value);
			formatData = higherFormats.Where(f => f.Value == min).Last().Key;

			//Return the size of the choosen format to downscale it later.
			var defaultSize = stream.FormatQualityLabels.LastOrDefault(f => stream.QualityLabel.StartsWith(f.Value.ToString())).Key;
			if (defaultSize != null)
				originalSize = new Size((int)defaultSize.Width, (int)defaultSize.Height);

			//Return the format.
			return formatData;
		}

		private static void CancelDownload(object sender, CancellationEventArgs e)
		{
			if (e.Restart)
				throw new DownloadRestartedException();
			e.CancelSource.Cancel();
		}

		//Saves the tags into the file
		public static void SaveTags(string filename, VideoData meta)
		{
			var tagfile = TagLib.File.Create(filename);

			tagfile.Tag.Title = meta.Title;

			if (!meta.Description.IsNullOrEmpty())
				tagfile.Tag.Description = meta.Description;

			if (!meta.Uploader.IsNullOrEmpty())
				tagfile.Tag.Performers = new[] { meta.Uploader };

			if (meta.UploadDate != null)
				tagfile.Tag.Year = (uint)meta.UploadDate.Value.Year;

			tagfile.Save();
		}

		/// <summary>
		/// Converts the downloaded audio file to the selected audio format (AppSettings) and/or removes the overhead a the start.
		/// </summary>
		/// <returns>The path of the converted file.</returns>
		private static async Task<string> ConvertAudioAsync(string inputfile, string format, TimeSpan startTime = default, StreamItemModel stream = null, CancellationToken cancellationToken = default)
		{
			StringBuilder argBuilder = new();
			argBuilder.Append($"-i \"{inputfile}\" ");
			if (!inputfile.EndsWith(format))
			{
				argBuilder.Append($"-y -loglevel \"repeat+info\" -movflags \"+faststart\" -vn ");
				argBuilder.Append(GetAudioConversionArgs());
			}
			argBuilder.Append($"-ss {startTime} ");

			string outputpath = Path.ChangeExtension(inputfile, format);
			for (int i = 2; File.Exists(outputpath); i++)
				outputpath = Path.ChangeExtension(outputpath, $"_{i}.{format}");
			argBuilder.Append($"\"{outputpath}\"");

			Debug.WriteLine(argBuilder.ToString());
			Engine ffmpeg = new(pathProvider.FFmpegPath);
			var meta = await ffmpeg.GetMetaDataAsync(new InputFile(inputfile), cancellationToken);

			if (stream != null)
			{
				ffmpeg.Complete += async (o, e) => await dispatcher.EnqueueAsync(() => stream.ConversionProgress = 100);
				ffmpeg.Error += async (o, e) => await dispatcher.EnqueueAsync(() => stream.ConversionProgress = 0);
				ffmpeg.Progress += async (o, e) => await dispatcher.EnqueueAsync(() => stream.ConversionProgress = (int)(e.ProcessedDuration / (meta.Duration - startTime) * 100));
			}

			await ffmpeg.ExecuteAsync(argBuilder.ToString(), cancellationToken);
			return outputpath;
		}

		/// <summary>
		/// Converts a file to MP4 (with H264 codec if enabled in settings) and returns the new path
		/// </summary>
		/// <returns>The path of the converted file</returns>
		private static async Task<string> ConvertToMp4Async(string inputfile, TimeSpan startTime = default, TimeSpan endTime = default, StreamItemModel stream = null, Size resolution = default, IMediaAnalysis videoMeta = null, CancellationToken cancellationToken = default)
		{
			string outputpath = Path.ChangeExtension(inputfile, ".converted.mp4");
			for (int i = 2; File.Exists(outputpath); i++)
				outputpath = Path.ChangeExtension(inputfile, $".converted_{i}.mp4");

			var input = new InputFile(inputfile);
			var output = new OutputFile(outputpath);
			Engine ffmpeg = new(pathProvider.FFmpegPath);

			var con = new ConversionOptions();
			if (AppSettings.Instance.AutoConvertToH264AfterDownload)
			{
				if (AppSettings.Instance.UseHardwareAcceleratedEncoding)
				{
					con.VideoCodec = AppSettings.Instance.HardwareEncoder switch
					{
						HardwareEncoder.Intel_QSV => VideoCodec.h264_qsv,
						HardwareEncoder.AMD_AMF => VideoCodec.h264_amf,
						HardwareEncoder.Nvidia_NVENC => VideoCodec.h264_nvenc,
						_ => VideoCodec.libx264
					};

					var meta = videoMeta ?? await FFProbe.AnalyseAsync(inputfile);
					con.VideoBitRate = (int)GlobalResources.CalculateVideoBitrate(inputfile, meta) / 1000;
				}
				else
					con.VideoCodec = VideoCodec.libx264;
			}
			else
				con.VideoCodec = VideoCodec.Default;

			StringBuilder extraArgs = new();
			extraArgs.Append($"-ss {startTime}");
			if (endTime != default)
				extraArgs.Append($" -to {endTime}");
			if (resolution != default)
				extraArgs.Append($" -vf scale={resolution.Width}:{resolution.Height}");
			con.ExtraArguments = extraArgs.ToString();

			if (!AppSettings.Instance.AutoSelectThreadsForConversion)
				con.Threads = AppSettings.Instance.MaxThreadCountForConversion;

			bool error = false;
			if (stream != null)
			{
				ffmpeg.Complete += async (o, e) => await dispatcher.EnqueueAsync(() => stream.ConversionProgress = 100);
				ffmpeg.Error += async (o, e) => await dispatcher.EnqueueAsync(() =>
				{
					stream.ConversionProgress = 0;
					error = true;
					Debug.WriteLine(e.Exception);
				});
				ffmpeg.Progress += async (o, e) => await dispatcher.EnqueueAsync(() =>
				{
					var totalDuration = e.TotalDuration - startTime;
					if (endTime != default)
						totalDuration -= totalDuration - endTime;

					stream.ConversionProgress = (int)(e.ProcessedDuration / totalDuration * 100);
				});
			}

			await ffmpeg.ConvertAsync(input, output, con, cancellationToken);

			//Fallback to software-encoding when hardware-encoding doen't work
			if (error && con.VideoCodec is VideoCodec.h264_nvenc or VideoCodec.h264_qsv or VideoCodec.h264_amf && AppSettings.Instance.FallBackToSoftwareEncoding)
			{
				error = false;
				con.VideoCodec = VideoCodec.libx264;
				con.VideoBitRate = null;
				await ffmpeg.ConvertAsync(input, output, con, cancellationToken);
			}

			if (error)
				stream.DownloadState = Enums.DownloadState.IsFailed;
			return outputpath;
		}


		private static async Task<Uri> MoveToDisk(string tempfile, string videoname, Enums.ItemType itemType, string directory = null, CancellationToken cancellationToken = default)
		{
			string extension = Path.GetExtension(tempfile);

			if (videoname.IsNullOrEmpty())
				videoname = "Download";

			string savefilepath = directory ?? (itemType == Enums.ItemType.audio ? AppSettings.Instance.DownloadsAudioSavePath : AppSettings.Instance.DownloadsVideoSavePath);

			Directory.CreateDirectory(savefilepath);
			int maxFilenameLength = 250 - (savefilepath.Length + extension.Length);
			string filename = Path.Combine(savefilepath, videoname.TrimToFilename(maxFilenameLength) + extension);

			if (!File.Exists(filename))
				await Task.Run(async () => await GlobalResources.MoveFileAsync(tempfile, filename, cancellationToken));

			else
			{
				string dir = Path.GetDirectoryName(filename);
				string name = Path.GetFileNameWithoutExtension(filename);

				//Increases the number until a possible file name is found
				for (int i = 2; File.Exists(filename); i++)
					filename = Path.Combine(dir, $"{name}_{i}{extension}");

				await Task.Run(async () => await GlobalResources.MoveFileAsync(tempfile, filename, cancellationToken));
			}
			return new Uri(filename);
		}

		public static async Task<IEnumerable<IVideo>> GetVideosFromPlaylistAsync(string playlistUrl, CancellationToken cToken = default)
		{
			//TODO: CHECK FOR VULNERABILITIES!
			Process ytdlProc = new();
			ytdlProc.StartInfo.CreateNoWindow = true;
			ytdlProc.StartInfo.FileName = pathProvider.YtDlPath;
			ytdlProc.StartInfo.RedirectStandardOutput = true;
			ytdlProc.StartInfo.Arguments = $"\"{playlistUrl}\" --flat-playlist --dump-json";
			ytdlProc.Start();

			string output = await ytdlProc.StandardOutput.ReadToEndAsync();
			if (!ytdlProc.HasExited)
				await ytdlProc.WaitForExitAsync(cToken);

			List<SelectableVideo> videos = new();
			foreach (var line in output.Split('\n'))
			{
				cToken.ThrowIfCancellationRequested();
				try { videos.Add(JsonConvert.DeserializeObject<SelectableVideo>(line.Trim())); }
				catch { continue; }
			}

			return videos.Where(v => v?.DurationAsFloatingNumber != null);
		}

		//Returns the resolutions from the videos.
		public static IEnumerable<string> GetResolutions(IEnumerable<StreamItemModel> videos)
		{
			ReadOnlySpan<int> validResolutions = stackalloc[] { 144, 144, 240, 360, 480, 720, 1080, 1440, 2160, 4320, 8640 };
			SortedSet<int> heights = new();
			SortedSet<int> correctHeights = new();
			List<string> resolutions = new();

			//Add video heights to a SortedSet
			foreach (var video in videos.Where(v => v != null))
				if (video.Video.Formats != null)
					foreach (var format in video.Video.Formats.Where(f => f != null))
						if (format.Height != null)
							heights.Add((int)format.Height);

			//Round up "non-default" resolutions
			foreach (int height in heights)
			{
				for (int i = 1; i < validResolutions.Length; i++)
				{
					if (height < validResolutions[i] && height > validResolutions[i - 1] || height == validResolutions[i])
					{
						correctHeights.Add(validResolutions[i]);
						break;
					}
					else if (height < validResolutions[i])
					{
						correctHeights.Add(validResolutions[i - 1]);
						break;
					}
				}
			}

			//Converts the heights to string values and adds a "p" at the end.
			foreach (int height in correctHeights.Reverse())
				resolutions.Add(height + "p");

			return resolutions;
		}

		public static async IAsyncEnumerable<SearchItemModel> GetSearchResultsAsync(string searchTerm, int maxResults = 20)
		{
			if (VideoSearchCancelSource.IsCancellationRequested)
				VideoSearchCancelSource = new();

			int counter = 0;
			await foreach (var result in youtube.Search.GetVideosAsync(searchTerm, VideoSearchCancelSource.Token))
			{
				if (counter == maxResults)
					break;

				yield return new SearchItemModel(result);
				counter++;
			}
		}
		public static CancellationTokenSource VideoSearchCancelSource { get; private set; } = new();


		private static string GetAudioConversionArgs() => AppSettings.Instance.DownloadsAudioFormat switch
		{
			AudioConversionFormat.Flac => "-acodec flac ",
			AudioConversionFormat.Opus => "-acodec libopus ",
			AudioConversionFormat.M4a => $"-acodec aac \"-bsf:a\" aac_adtstoasc -aq {GetAudioQuality("aac").ToString().Replace(',', '.')} ",
			AudioConversionFormat.Mp3 => $"-acodec libmp3lame -aq {GetAudioQuality("libmp3lame").ToString().Replace(',', '.')} ",
			AudioConversionFormat.Vorbis => $"-acodec libvorbis -aq {GetAudioQuality("libvorbis").ToString().Replace(',', '.')} ",
			_ => $"-f {AppSettings.Instance.DownloadsAudioFormat.ToString().ToLower()} "
		};

		private static readonly Dictionary<string, (float, int)> audioQualityDict = new()
		{
			{ "libmp3lame", (10, 0) },
			{ "libvorbis", (0, 10) },
            // FFmpeg's AAC encoder does not have an upper limit for the value of -aq.
            // Experimentally, with values over 4, bitrate changes were minimal or non-existent
            { "aac", (0.1f, 4) }
		};

		//Preferred quality for audio-conversion (up to 10).
		private const float preferredQuality = 5f;

		private static float GetAudioQuality(string codec)
			=> audioQualityDict[codec].Item2 + (audioQualityDict[codec].Item1 - audioQualityDict[codec].Item2) * (preferredQuality / 10);
	}
}
