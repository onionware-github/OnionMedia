/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using OnionMedia.Core.Classes;
using OnionMedia.Core.Extensions;
using OnionMedia.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace OnionMedia.Core.Models
{
    [ObservableObject]
    public partial class StreamItemModel
    {
        /// <summary>
        /// Initializes a new StreamItemModel.
        /// </summary>
        /// <param name="video">The video to get informations from.</param>
        public StreamItemModel(RunResult<VideoData> video)
        {
            if (video.Data == null)
                throw new ArgumentNullException("video.Data is null!");

            if (video.Data.IsLive == true)
                throw new NotSupportedException("Livestreams cannot be downloaded.");

            Video = video.Data;
            if (Video.Thumbnail.IsNullOrEmpty())
                Video.Thumbnail = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ac/No_image_available.svg/300px-No_image_available.svg.png";
            Duration = new TimeSpan(0, 0, (int)Video.Duration);
            TimeSpanGroup = new TimeSpanGroup(Duration);

            //Add video formats to the list
            if (Video.Formats != null)
                foreach (var format in Video.Formats.Where(f => f.Width != null && f.Height != null && f.FrameRate != null))
                    FormatQualityLabels.Add(format, ((int)format.Height).RoundUpToNearestNeighbor(validResolutions));

            TimeSpanGroup.PropertyChanged += (o, e) => OnPropertyChanged(nameof(CustomTimes));
            CancelSource = new();
            SetProgressToDefault();
        }

        private readonly IToastNotificationService toastNotificationService = IoC.Default.GetService<IToastNotificationService>();

        public void SetProgressToDefault()
        {
            if (CancelSource.IsCancellationRequested)
                CancelSource = new();

            if (DownloadProgress != null)
                DownloadProgress.ProgressChanged -= OnProgressChanged;

            DownloadProgress = new();
            ProgressInfo = new();

            DownloadState = Enums.DownloadState.IsWaiting;
            DownloadProgress.ProgressChanged += OnProgressChanged;
        }

        protected void OnProgressChanged(object sender, DownloadProgress e)
        {
            downloadProgress = e;
            var matchResult = Regex.Match(e.Data ?? string.Empty, GlobalResources.FFMPEGTIMEFROMOUTPUTREGEX);

            if (matchResult.Success)
                ProgressInfo.Progress = (int)(TimeSpan.Parse(matchResult.Value.Remove(0, 5)) / (TimeSpanGroup.EndTime - TimeSpanGroup.StartTime) * 100);
            else if (e.Progress != 0)
                ProgressInfo.Progress = (int)(downloadProgress.Progress * 100);

            ProgressInfo.TotalSize = downloadProgress.TotalDownloadSize;
            ProgressInfo.DownloadState = downloadProgress.State;
            //Debug.WriteLine((byte)(downloadProgress.Progress * 100) + " - " + ProgressInfo.Progress);

            OnPropertyChanged(nameof(ProgressInfo));
            OnPropertyChanged(nameof(Downloading));
            OnPropertyChanged(nameof(Converting));
            ProgressChangedEventHandler?.Invoke(this, new());

            if (!isUpdating && DownloadState is Enums.DownloadState.IsLoading)
                _ = UpdateProgressInfoAsync();
        }

        private bool isUpdating;
        private DownloadProgress downloadProgress;
        private async Task UpdateProgressInfoAsync()
        {
            Debug.WriteLine("Starting update task...");
            isUpdating = true;
            while (DownloadState is Enums.DownloadState.IsLoading)
            {
                ProgressInfo.DownloadSpeed = downloadProgress.DownloadSpeed;
                OnPropertyChanged(nameof(ProgressInfo));
                await Task.Delay(1000);
            }
            isUpdating = false;
            Debug.WriteLine("Finish update task");
        }

        /// <summary>
        /// Contains informations of the video
        /// </summary>
        public VideoData Video { get; }
        public TimeSpan Duration { get; }
        public TimeSpanGroup TimeSpanGroup { get; }
        public bool CustomTimes => TimeSpanGroup != null && !TimeSpanGroup.StartTime.Equals(TimeSpan.Zero) || !TimeSpanGroup.EndTime.Equals(TimeSpanGroup.Duration);

        /// <summary>
        /// The downloading progress
        /// </summary>
        public Progress<DownloadProgress> DownloadProgress { get; set; }

        /// <summary>
        /// The conversion progress after download in %
        /// </summary>
        [ObservableProperty]
        private int conversionProgress;

        /// <summary>
        /// Contains informations about the downloading progress
        /// </summary>
        public ProgressModel ProgressInfo { get; set; }

        /// <summary>
        /// The path to the downloaded video
        /// </summary>
        public Uri Path { get; set; }

        /// <summary>
        /// The CancellationTokenSource to cancel the download
        /// </summary>
        public CancellationTokenSource CancelSource { get; protected set; }

        /// <summary>
        /// Indicates whether the video is being downloaded
        /// </summary>
        [ObservableProperty]
        private bool downloading;

        [ObservableProperty]
        private bool converting;

        [ObservableProperty]
        private bool moving;

        public bool Success => DownloadState is Enums.DownloadState.IsDone;
        public bool Failed => DownloadState is Enums.DownloadState.IsFailed;

        /// <summary>
        /// Shows the downloadstate
        /// </summary>
        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(ProgressInfo))]
        [AlsoNotifyChangeFor(nameof(Downloading))]
        [AlsoNotifyChangeFor(nameof(Success))]
        [AlsoNotifyChangeFor(nameof(Failed))]
        private Enums.DownloadState downloadState;


        /// <summary>
        /// The dialog that matches the download status
        /// </summary>
        [ObservableProperty]
        private string progressDialog;


        /// <summary>
        /// Displays a notification when the download has completed
        /// </summary>
        public void ShowToast()
        {
            toastNotificationService.SendDownloadDoneNotification(Video, Path.OriginalString);
        }

        /// <summary>
        /// The quality for the video to download
        /// </summary>
        public string QualityLabel { get; set; }
        /// <summary>
        /// The height for the video to download
        /// </summary>
        public int GetVideoHeight => Convert.ToInt32(QualityLabel.Remove(QualityLabel.Length - 1));
        /// <summary>
        /// The format for the video to download
        /// </summary>
        public string Format => $"bestvideo[height<={GetVideoHeight}]+bestaudio[ext=m4a]/best[height<={GetVideoHeight}]/best";

        /// <summary>
        /// Contains foreach video format a quality label that represents the resolution
        /// TKey = Format object
        /// TValue = Rounded height (e.g. 360)
        /// </summary>
        public Dictionary<FormatData, int> FormatQualityLabels { get; } = new();

        /// <summary>
        /// Occurs when the download progress get changed
        /// </summary>
        public event EventHandler ProgressChangedEventHandler;
        /// <summary>
        /// Occurs when the download has finished
        /// </summary>
        public event EventHandler FinishedEventHandler;
        /// <summary>
        /// Occurs when the download has cancelled
        /// </summary>
        public event EventHandler<CancellationEventArgs> CancelEventHandler;

        /// <summary>
        /// Raises the CancelEvent
        /// </summary>
        /// <param name="restart">Restart the download</param>
        public void RaiseCancel(bool restart = false)
        {
            if (!restart)
                ProgressInfo.IsCancelledOrFailed = true;
            CancelEventHandler?.Invoke(this, new CancellationEventArgs(CancelSource, restart));
        }

        /// <summary>
        /// Raise the FinishedEvent
        /// </summary>
        public void RaiseFinished()
            => FinishedEventHandler?.Invoke(this, new EventArgs());

        static readonly int[] validResolutions = { 144, 240, 360, 480, 720, 1080, 1440, 2160, 4320, 8640 };
    }
}