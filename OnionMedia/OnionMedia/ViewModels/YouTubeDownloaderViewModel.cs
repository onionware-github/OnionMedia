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
using System.Threading;
using System.ComponentModel;

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

            DownloadFileCommand = new(async qualityLabel => await DownloadVideosAsync(Videos, GetMP4, qualityLabel));

            RemoveCommand = new(async index => await RemoveVideoAsync());

            RestartDownloadCommand = new(video => video.RaiseCancel(true), video => video != null && video.DownloadState == DownloadState.IsLoading);
            DownloadFileCommand.PropertyChanged += (o,e) => UpdateProgressStateProperties();
            AddVideoCommand.PropertyChanged += (o, e) => UpdateProgressStateProperties();
            AddSearchedVideo.PropertyChanged += (o, e) => UpdateProgressStateProperties();
            Videos.CollectionChanged += (o, e) => UpdateProgressStateProperties();
        }


        public AsyncRelayCommand<string> DownloadFileCommand { get; }
        public AsyncRelayCommand<string> AddVideoCommand { get; }
        public AsyncRelayCommand<SearchItemModel> AddSearchedVideo { get; }
        public RelayCommand<StreamItemModel> RestartDownloadCommand { get; }
        public RelayCommand<int> RemoveCommand { get; }

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

        //Search a video or get a video from a url and add it to the queue.
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
                {
                    await RefreshResultsAsync(videolink);
                    return;
                }

                ScanVideoCount++;
                var data = await DownloaderMethods.downloadClient.RunVideoDataFetch(videolink);

                if (data.Data == null && urlClone == SearchTerm)
                {
                    VideoNotFound = true;
                    ScanVideoCount--;
                    return;
                }

                var video = new StreamItemModel(data);
                video.Video.Url = videolink;

                if (Videos.Any(v => v.Video.ID == video.Video.ID))
                {
                    ScanVideoCount--;
                    return;
                }

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

                ScanVideoCount--;
                OnPropertyChanged(nameof(QueueIsEmpty));
                OnPropertyChanged(nameof(QueueIsNotEmpty));
                OnPropertyChanged(nameof(MultipleVideos));
            }
            catch (Exception ex)
            {
                ScanVideoCount--;
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

                    //TODO: What is that piece of code?! (i cant remember lol)
                    case ArgumentOutOfRangeException:
                        throw new ArgumentOutOfRangeException("This bug should be fixed...", ex);

                    case ArgumentNullException:
                        await RefreshResultsAsync(string.Empty);
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

            int finishedCount = 0;
            List<Task> tasks = new();
            SemaphoreSlim queue = new(AppSettings.Instance.SimultaneousOperationCount, AppSettings.Instance.SimultaneousOperationCount);
            StreamItemModel[] items = videos.ToArray();
            StreamItemModel loadedVideo = null;
            foreach (var video in items)
            {
                video.ProgressInfo.IsDone = false;
                video.ProgressInfo.Progress = 0;
                video.QualityLabel = qualityLabel;
            }
            foreach (var video in items)
            {
                if (!videos.Contains(video) || video.DownloadState == DownloadState.IsCancelled) continue;
                await queue.WaitAsync();

                if (!videos.Contains(video) || video.DownloadState == DownloadState.IsCancelled) continue;

                video.FinishedEventHandler += (o, e) =>
                {
                    loadedVideo = (StreamItemModel)o;
                    finishedCount++;
                };

                tasks.Add(DownloaderMethods.DownloadStreamAsync(video, getMp4).ContinueWith(t => queue.Release()));
            }
            await Task.WhenAll(tasks);

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

                if (items.All(v => v.Path != null && Path.GetDirectoryName(v.Path.OriginalString) == Path.GetDirectoryName(loadedVideo.Path.OriginalString)))
                {
                    StringBuilder files = new();
                    var filenames = items.Where(v => v.Path != null && v.DownloadState is DownloadState.IsDone && File.Exists(v.Path.OriginalString)).Select(v => Path.GetFileName(v.Path.OriginalString));
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

        /// <summary>
        /// The number of videos that get scanned in the moment.
        /// </summary>
        [ObservableProperty]
        private int scanVideoCount;

        public bool QueueIsEmpty => !Videos.Any();
        public bool QueueIsNotEmpty => Videos.Any() && !DownloadFileCommand.IsRunning;
        public bool ReadyToDownload => QueueIsNotEmpty && ScanVideoCount == 0;
        public bool AnyResults => SearchResults.Any();
        public bool AddingVideo => AddVideoCommand.IsRunning || AddSearchedVideo.IsRunning;
        private void UpdateProgressStateProperties()
        {
            OnPropertyChanged(nameof(AddingVideo));
            OnPropertyChanged(nameof(QueueIsNotEmpty));
            OnPropertyChanged(nameof(ReadyToDownload));
        }

        public bool MultipleVideos => Videos.Count > 1;

        public bool SelectionIsInRange(IEnumerable<StreamItemModel> collection, int index, bool inverseResult = false)
        {
            if (!inverseResult)
                return index < collection.Count() && index >= 0 && collection.Any();
            return !(index < collection.Count() && index >= 0 && collection.Any());
        }
    }
}
