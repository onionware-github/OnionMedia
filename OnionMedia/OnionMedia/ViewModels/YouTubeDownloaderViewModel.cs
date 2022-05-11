/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Notifications;

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using OnionMedia.Core.Models;
using YoutubeExplode.Exceptions;
using OnionMedia.Core.Enums;
using System.Net.Http;
using System.Text;
using System.IO;
using OnionMedia.Core.Classes;
using OnionMedia.Helpers;
using OnionMedia.Core.Extensions;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Controls;

namespace OnionMedia.ViewModels
{
    [ObservableObject]
    public sealed partial class YouTubeDownloaderViewModel
    {
        public YouTubeDownloaderViewModel()
        {
            SearchResults.CollectionChanged += (o, e) => OnPropertyChanged(nameof(AnyResults));
            Videos.CollectionChanged += OnProgressChanged;

            AddVideoCommand = new(async link => await FillInfosAsync(link), link => !DownloadFileCommand.IsRunning);

            AddSearchedVideo = new(async item => await FillInfosAsync(item.Url), item => !DownloadFileCommand.IsRunning);

            DownloadFileCommand = new(async qualityLabel => await DownloadVideosAsync(Videos, GetMP4, qualityLabel), str => !Videos.Any(v => v.DownloadState is DownloadState.IsLoading));

            RemoveCommand = new(async index => await RemoveVideoAsync());

            RestartDownloadCommand = new(video => video.RaiseCancel(true), video => video != null && video.DownloadState == DownloadState.IsLoading);
            DownloadFileCommand.PropertyChanged += (o, e) => OnPropertyChanged(nameof(QueueIsNotEmpty));
        }

        public AsyncRelayCommand<string> DownloadFileCommand { get; }
        public RelayCommand<string> AddVideoCommand { get; }
        public RelayCommand<int> RemoveCommand { get; }
        public RelayCommand<SearchItemModel> AddSearchedVideo { get; }
        public RelayCommand<StreamItemModel> RestartDownloadCommand { get; }

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(ResolutionsAvailable))]
        private bool getMP4 = true;

        [ObservableProperty]
        private bool validUrl;

        [ObservableProperty]
        private bool videoNotFound;

        public bool ResolutionsAvailable => GetMP4 && Videos.Any() && Resolutions.Any();

        public string SearchTerm
        {
            get => searchTerm;
            set
            {
                if (!SetProperty(ref searchTerm, value)) return;
                VideoNotFound = false;
            }
        }
        private string searchTerm;
        public ObservableCollection<SearchItemModel> SearchResults { get; } = new();

        public ObservableCollection<StreamItemModel> Videos { get; set; } = new();
        public ObservableCollection<string> Resolutions { get; set; } = new();

        private StreamItemModel selectedVideo;
        public StreamItemModel SelectedVideo
        {
            get => selectedVideo;
            set
            {
                if (selectedVideo != value && value != null)
                {
                    selectedVideo = value;
                    OnPropertyChanged();
                }
            }
        }

        private readonly StringBuilder sb = new();
        private string previouslySelected;
        private string selectedQuality;
        public string SelectedQuality
        {
            get => selectedQuality;
            set
            {
                if (selectedQuality != value)
                {
                    if (previouslySelected == null)
                    {
                        selectedQuality = value;
                        previouslySelected = selectedQuality;
                    }
                    else
                    {
                        sb.Append(selectedQuality);
                        previouslySelected = sb.ToString();
                        selectedQuality = value;
                        sb.Clear();
                    }
                    OnPropertyChanged();
                }
            }
        }

        //Use an old version of yt-dl to get thumbnails
        private static readonly YoutubeDLSharp.YoutubeDL ytDL = new()
        {
            YoutubeDLPath = GlobalResources.Installpath + @"\ExternalBinaries\yt-dl.exe",
            OutputFolder = GlobalResources.DownloaderTempdir,
            OverwriteFiles = true
        };

        private async Task FillInfosAsync(string videolink)
        {
            if (VideoNotFound)
            {
                VideoNotFound = false;
                VideoNotFound = true;
                return;
            }

            string urlClone = (string)videolink.Clone();
            SearchResults.Clear();

            if (string.IsNullOrWhiteSpace(videolink))
                return;

            bool validUri = Regex.IsMatch(videolink, GlobalResources.URLREGEX);
            //Remove the "feature=share" from shared yt-shorts urls.
            if (videolink.Contains("youtube.com/shorts/"))
                videolink = videolink.Replace("?feature=share", string.Empty);

            try
            {
                if (!validUri)
                    throw new ArgumentException(null, nameof(videolink));

                var data = await ytDL.RunVideoDataFetch(videolink);

                //Run a check again in case of an age-restricted video
                if (data.Data == null && validUri)
                    data = await DownloaderMethods.downloadClient.RunVideoDataFetch(videolink);

                if (data.Data == null && urlClone == SearchTerm)
                {
                    VideoNotFound = true;
                    return;
                }

                var video = new StreamItemModel(data);
                video.Video.Url = videolink;


                if (!Videos.Any(v => v.Video.ID == video.Video.ID))
                    Videos.Add(video);

                video.ProgressChangedEventHandler += OnProgressChanged;
                SelectedVideo = video;

                if (urlClone == SearchTerm)
                    SearchTerm = string.Empty;

                Resolutions = new ObservableCollection<string>(DownloaderMethods.GetResolutions(Videos));
                OnPropertyChanged(nameof(Resolutions));
                OnPropertyChanged(nameof(ResolutionsAvailable));

                //TODO Videos ohne QualityLabels filtern
                if (previouslySelected != null)
                    SelectedQuality = previouslySelected;
                else if (Resolutions.Any())
                    SelectedQuality = Resolutions[0];
                else
                    SelectedQuality = null;

                OnPropertyChanged(nameof(QueueIsEmpty));
                OnPropertyChanged(nameof(QueueIsNotEmpty));
                OnPropertyChanged(nameof(MultipleVideos));
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case InvalidOperationException:
                        Debug.WriteLine("InvalidOperation triggered");
                        break;

                    case VideoUnavailableException:
                        await RefreshResultsAsync(videolink);
                        break;

                    case YoutubeExplodeException:
                        Debug.WriteLine("Video error");
                        break;
                    //TODO: What is that?
                    case ArgumentOutOfRangeException:
                        throw new ArgumentOutOfRangeException("This bug should be fixed...", ex);

                    case ArgumentNullException:
                        await RefreshResultsAsync(string.Empty);
                        break;

                    case ArgumentException:
                        await RefreshResultsAsync(videolink);
                        break;

                    case NotSupportedException:
                        await new ContentDialog()
                        {
                            Title = "livestreamDlgTitle".GetLocalized(),
                            Content = "livestreamDlgContent".GetLocalized(),
                            XamlRoot = GlobalResources.XamlRoot,
                            PrimaryButtonText = "OK"
                        }.ShowAsync();
                        break;

                    case HttpRequestException:
                        Debug.WriteLine("No internet connection!");
                        break;
                }
            }
        }

        private void OnProgressChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(ResolutionsAvailable));
            if (Videos.Any() && Videos.All(v => v.ProgressInfo.DownloadState == YoutubeDLSharp.DownloadState.Success))
            {
                DownloadProgress = 100;
                return;
            }

            int progress = 0;
            int videocount = Videos.Count;

            //Get progress from all videos
            foreach (var video in Videos)
                progress += video.ProgressInfo.Progress;

            if (videocount == 0)
                DownloadProgress = 0;
            else
                DownloadProgress = progress / videocount;
        }

        private async Task RemoveVideoAsync()
        {
            try
            {
                if (SelectedVideo.DownloadState == DownloadState.IsLoading)
                    SelectedVideo.RaiseCancel();

                if (Videos.Count <= 1)
                    Videos.Clear();
                else
                    Videos.Remove(SelectedVideo);

                OnPropertyChanged(nameof(QueueIsEmpty));
                OnPropertyChanged(nameof(QueueIsNotEmpty));
                OnPropertyChanged(nameof(MultipleVideos));

                if (Videos.Any())
                    SelectedVideo = Videos[^1];

                Resolutions = new ObservableCollection<string>(DownloaderMethods.GetResolutions(Videos));
                OnPropertyChanged(nameof(Resolutions));
                OnPropertyChanged(nameof(ResolutionsAvailable));

                if (previouslySelected != null && Resolutions.Contains(previouslySelected))
                    SelectedQuality = previouslySelected;
                else if (Resolutions.Any())
                    SelectedQuality = Resolutions[0];
                else
                    SelectedQuality = null;
            }
            catch (InvalidOperationException) { Debug.WriteLine("InvalidOperation triggered"); }
            await Task.CompletedTask;
        }


        private static async Task DownloadVideosAsync(IList<StreamItemModel> videos, bool getMp4, string qualityLabel)
        {
            if (videos == null || !videos.Any())
                throw new ArgumentException("videos is null or empty.");

            StreamItemModel loadedVideo = videos[0];
            int finishedCount = 0;

            var tasks = new Task[videos.Count];
            YoutubeDLSharp.YoutubeDL youtubeClient = new((byte)AppSettings.Instance.SimultaneousOperationCount)
            {
                FFmpegPath = GlobalResources.FFmpegPath,
                YoutubeDLPath = GlobalResources.YtDlPath,
                OutputFolder = GlobalResources.DownloaderTempdir,
                OverwriteFiles = true
            };
            for (int i = 0; i < videos.Count; i++)
            {
                videos[i].ProgressInfo.IsDone = false;
                videos[i].ProgressInfo.Progress = 0;
                videos[i].QualityLabel = qualityLabel;
                videos[i].FinishedEventHandler += (o, e) =>
                {
                    loadedVideo = (StreamItemModel)o;
                    finishedCount++;
                    Debug.WriteLine($"{loadedVideo.Video.Title} was added to the queue. It has {finishedCount} items now.");
                };
                tasks[i] = DownloaderMethods.DownloadStreamAsync(videos[i], getMp4, youtubeClient);
            }
            await Task.WhenAll(tasks.Where(t => t != null));
            Debug.WriteLine("Downloadtask is done.");

            foreach (var dir in Directory.GetDirectories(GlobalResources.DownloaderTempdir))
            {
                try { Directory.Delete(dir, true); }
                catch { /* Dont crash if a directory cant be deleted */ }
            }
            if (!AppSettings.Instance.SendMessageAfterDownload) return;

            if (finishedCount == 1)
            {
                loadedVideo.ShowToast();
            }
            else if (finishedCount > 1)
            {
                var dialog = new ToastContentBuilder()
                .AddText("downloadFinished".GetLocalized())
                .AddText($"{finishedCount} {"videosDownloaded".GetLocalized()}");

                if (videos.All(v => v.Path != null && Path.GetDirectoryName(v.Path.OriginalString) == Path.GetDirectoryName(loadedVideo.Path.OriginalString)))
                {
                    StringBuilder files = new();
                    var filenames = videos.Where(v => v.Path != null && v.DownloadState is DownloadState.IsDone && File.Exists(v.Path.OriginalString)).Select(v => Path.GetFileName(v.Path.OriginalString));
                    foreach (var file in filenames)
                        files.AppendLine(file);

                    dialog.AddButton(new ToastButton()
                .SetContent("openFolder".GetLocalized())
                .AddArgument("action", "open path")
                .AddArgument("folderpath", loadedVideo.Path.OriginalString)
                .AddArgument("filenames", files.ToString())
                .SetBackgroundActivation());
                }


                dialog.Show(toast =>
                {
                    toast.Group = "downloadMsgs";
                    toast.Tag = "0";
                });
            }
        }

        private (string query, ICollection<SearchItemModel> results) lastSearch = (string.Empty, new Collection<SearchItemModel>());
        private async Task RefreshResultsAsync(string searchTerm)
        {
            if (searchTerm.Equals(lastSearch.query) && lastSearch.results.Any())
            {
                SearchResults.Replace(lastSearch.results);
                return;
            }
            try
            {
                SearchResults.Clear();
                await foreach (var result in DownloaderMethods.GetSearchResultsAsync(searchTerm))
                    SearchResults.Add(result);
                lastSearch.query = searchTerm;
                lastSearch.results.Replace(SearchResults);
            }
            catch (HttpRequestException)
            {
                Debug.WriteLine("No internet connection!");
            }
            catch (TaskCanceledException) { }
        }

        [ICommand]
        private void ClearResults()
        {
            if (!DownloaderMethods.VideoSearchCancelSource.IsCancellationRequested)
                DownloaderMethods.VideoSearchCancelSource.Cancel();

            if (!SearchResults.Any()) return;
            SearchResults.Clear();
            lastSearch = (string.Empty, new Collection<SearchItemModel>());
        }

        [ObservableProperty]
        private int downloadProgress;

        public bool QueueIsEmpty => !Videos.Any();
        public bool QueueIsNotEmpty => Videos.Any() && !DownloadFileCommand.IsRunning;
        public bool AnyResults => SearchResults.Any();

        public bool MultipleVideos => Videos.Count > 1;

        public bool SelectionIsInRange(IEnumerable<StreamItemModel> collection, int index, bool inverseResult = false)
        {
            if (!inverseResult)
                return index < collection.Count() && index >= 0 && collection.Any();
            return !(index < collection.Count() && index >= 0 && collection.Any());
        }
    }
}
