/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.ObjectModel;
using System.Threading;
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
using OnionMedia.Core.Extensions;
using System.Text.RegularExpressions;
using TextCopy;
using OnionMedia.Core.Services;
using YoutubeDLSharp.Options;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;

namespace OnionMedia.Core.ViewModels
{
    [ObservableObject]
    public sealed partial class YouTubeDownloaderViewModel
    {
        public YouTubeDownloaderViewModel(IDialogService dialogService, IDownloaderDialogService downloaderDialogService, IDispatcherService dispatcher, INetworkStatusService networkStatusService, IToastNotificationService toastNotificationService, IPathProvider pathProvider, ITaskbarProgressService taskbarProgressService, IWindowClosingService windowClosingService, IFiletagEditorDialog filetagDialogService)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.downloaderDialogService = downloaderDialogService ?? throw new ArgumentNullException(nameof(downloaderDialogService));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.toastNotificationService = toastNotificationService ?? throw new ArgumentNullException(nameof(toastNotificationService));
            this.pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
            this.filetagDialogService = filetagDialogService ?? throw new ArgumentNullException(nameof(filetagDialogService));
            this.networkStatusService = networkStatusService;
            this.taskbarProgressService = taskbarProgressService;

            SearchResults.CollectionChanged += (o, e) => OnPropertyChanged(nameof(AnyResults));
            Videos.CollectionChanged += OnProgressChanged;

            AddVideoCommand = new(async link => await FillInfosAsync(link), link => !DownloadFileCommand.IsRunning);

            AddSearchedVideo = new(async item => await FillInfosAsync(item.Url), item => !DownloadFileCommand.IsRunning);

            DownloadFileCommand = new(async qualityLabel => await DownloadVideosAsync(Videos, GetMP4, qualityLabel));

            RemoveCommand = new(async index => await RemoveVideoAsync());

            RestartDownloadCommand = new(video => video.RaiseCancel(true), video => video != null && video.DownloadState == DownloadState.IsLoading);
            DownloadFileCommand.PropertyChanged += (o, e) => UpdateProgressStateProperties();
            AddVideoCommand.PropertyChanged += (o, e) => UpdateProgressStateProperties();
            AddSearchedVideo.PropertyChanged += (o, e) => UpdateProgressStateProperties();
            Videos.CollectionChanged += (o, e) => UpdateProgressStateProperties();
            windowClosingService.Closed += (o, e) => CancelAll();
            networkAvailable = this.networkStatusService?.IsNetworkConnectionAvailable() ?? true;
            if (this.networkStatusService != null)
                this.networkStatusService.ConnectionStateChanged += (o, e) => this.dispatcher.Enqueue(() => NetworkAvailable = e);
        }

        private readonly IDialogService dialogService;
        private readonly IDownloaderDialogService downloaderDialogService;
        private readonly IFiletagEditorDialog filetagDialogService;
        private readonly IDispatcherService dispatcher;
        private readonly INetworkStatusService networkStatusService;
        private readonly IToastNotificationService toastNotificationService;
        private readonly IPathProvider pathProvider;
        private readonly ITaskbarProgressService taskbarProgressService;
        private static readonly IUrlService urlService = IoC.Default.GetService<IUrlService>() ?? throw new ArgumentNullException();

        public static AsyncRelayCommand<string> OpenUrlCommand { get; } = new(async url => await urlService.OpenUrlAsync(url));
        public AsyncRelayCommand<string> DownloadFileCommand { get; }
        public AsyncRelayCommand<string> AddVideoCommand { get; }
        public AsyncRelayCommand<SearchItemModel> AddSearchedVideo { get; }
        public RelayCommand<StreamItemModel> RestartDownloadCommand { get; }
        public RelayCommand<int> RemoveCommand { get; }

        public event EventHandler<bool> DownloadDone;

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(ResolutionsAvailable))]
        private bool getMP4 = true;

        [ObservableProperty]
        private bool validUrl;

        [ObservableProperty]
        private bool videoNotFound;

        [ObservableProperty]
        private bool networkAvailable;

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
        private string searchTerm = string.Empty;
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
                if (selectedQuality == value) return;

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

        //Search a video or get a video from a url and add it to the queue.
        private async Task FillInfosAsync(string videolink, bool allowPlaylists = true)
        {
            if (VideoNotFound)
            {
                VideoNotFound = false;
                VideoNotFound = true;
                return;
            }

            string urlClone = (string)(videolink?.Clone() ?? string.Empty);

            //Cancel searching and clear results
            if (searchProcesses > 0 && !DownloaderMethods.VideoSearchCancelSource.IsCancellationRequested)
                DownloaderMethods.VideoSearchCancelSource.Cancel();
            SearchResults.Clear();

            if (string.IsNullOrWhiteSpace(videolink))
                return;

            bool validUri = Regex.IsMatch(videolink, GlobalResources.URLREGEX);
            bool isYoutubePlaylist = validUri && allowPlaylists && IsYoutubePlaylist(urlClone);

            //Remove the "feature=share" from shared yt-shorts urls.
            if (videolink.Contains("youtube.com/shorts/"))
                videolink = videolink.Replace("?feature=share", string.Empty);

            try
            {
                if (!validUri)
                {
                    await RefreshResultsAsync(videolink.Clone() as string);
                    return;
                }

                if (isYoutubePlaylist && (AppSettings.Instance.VideoAddMode is VideoAddMode.AddPlaylist || AppSettings.Instance.VideoAddMode is VideoAddMode.AskForVideoAddMode && await AskForPlaylistAsync()))
                {
                    ScanVideoCount++;
                    try
                    {
                        var videos = await DownloaderMethods.GetVideosFromPlaylistAsync(urlClone);

                        var urls = (await downloaderDialogService.ShowPlaylistSelectorDialogAsync(videos)).Select(v => v.Url);
                        var videosToAdd = await GetVideosAsync(urls);
                        var sortedVideos = videosToAdd.OrderBy(video => videos.IndexOf(v => v.Url == video.Video.Url));

                        AddVideos(sortedVideos);
                        if (Videos.Any())
                            SelectedVideo = Videos[^1];
                        return;
                    }
                    finally
                    {
                        ScanVideoCount--;
                    }
                }

                ScanVideoCount++;
                var data = await DownloaderMethods.downloadClient.RunVideoDataFetch(videolink, overrideOptions: OptionSet.FromString(new[] {"--extractor-args \"youtube:player_client=android,web\""}));

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

                lock (Videos)
                    Videos.Add(video);

                video.ProgressChangedEventHandler += OnProgressChanged;
                SelectedVideo = video;

                if (urlClone == SearchTerm)
                    SearchTerm = string.Empty;

                Resolutions = new ObservableCollection<string>(DownloaderMethods.GetResolutions(Videos));
                OnPropertyChanged(nameof(Resolutions));
                OnPropertyChanged(nameof(ResolutionsAvailable));

                //TODO Filter videos without QualityLabels
                if (!string.IsNullOrEmpty(previouslySelected))
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

                    //TODO: Check what is that piece of code doing?! (i cant remember)
                    case ArgumentOutOfRangeException:
                        throw new ArgumentOutOfRangeException("This bug should be fixed...", ex);

                    case ArgumentNullException:
                        SearchResults.Clear();
                        break;

                    case NotSupportedException:
                        await dialogService.ShowInfoDialogAsync("livestreamDlgTitle".GetLocalized(), "livestreamDlgContent".GetLocalized(), "OK");
                        break;

                    case HttpRequestException:
                        Debug.WriteLine("No internet connection!");
                        break;
                }
            }
        }

        [ICommand]
        private async Task CopyUrlAsync(string url)
        {
	        await ClipboardService.SetTextAsync(url);
        }

        [ICommand]
        private async Task ShowLogAsync(StreamItemModel video)
        {
	        if (string.IsNullOrEmpty(video.DownloadLog))
	        {
		        await dialogService.ShowInfoDialogAsync(video.Video.Title, "emptyLog".GetLocalized(), "close".GetLocalized());
		        return;
	        }

	        bool? storeAsFile = await dialogService.ShowInteractionDialogAsync(video.Video.Title, video.DownloadLog, "save".GetLocalized(), "copy".GetLocalized(), "close".GetLocalized());
	        if (storeAsFile is null)
	        {
		        return;
	        }
	        if (storeAsFile is false)
	        {
		        await ClipboardService.SetTextAsync(video.DownloadLog);
		        return;
	        }

	        Dictionary<string, IEnumerable<string>> types = new()
	        {
		        { "", new[] { ".txt" } }
	        };
			string filepath = await dialogService.ShowSaveFilePickerDialogAsync("log.txt", types, DirectoryLocation.Documents);
	        if (filepath != null)
	        {
		        await File.WriteAllTextAsync(filepath, video.DownloadLog);
	        }
        }

        [ICommand]
        private async Task DownloadThumbnailAsync(StreamItemModel video)
        {
	        string tempPath = Path.GetTempFileName();

	        var types = new Dictionary<string, IEnumerable<string>>()
	        {
		        { "Portable Network Graphics", new[] { ".png" } },
		        { "JPEG", new[] { ".jpg", ".jpeg" } }
			};
	        string filepath = await dialogService.ShowSaveFilePickerDialogAsync(video.Video.Title.TrimToFilename(int.MaxValue), types, DirectoryLocation.Pictures);
	        if (filepath is null)
	        {
		        return;
	        }

	        string format = Path.GetExtension(filepath) switch
	        {
		        ".jpg" => "jpg",
		        ".jpeg" => "jpg",
		        _ => "png"
	        };

	        await DownloaderMethods.DownloadThumbnailAsync(video.Video.Url, tempPath, format);
            File.Move(tempPath, filepath, true);
        }

        [ICommand]
        private async Task EditTagsAsync(StreamItemModel video)
        {
	        FileTags tags = video.CustomTags ?? new()
	        {
		        Title = video.Video.Title,
		        Description = video.Video.Description,
		        Artist = video.Video.Uploader,
		        Year = video.Video.UploadDate.HasValue ? (uint)video.Video.UploadDate.Value.Year : 0,
	        };
	        var finalTags = await filetagDialogService.ShowTagEditorDialogAsync(tags);
	        if (finalTags is null)
	        {
		        return;
	        }

            video.CustomTags = finalTags;
            if (video.DownloadState == DownloadState.IsDone && File.Exists(video.Path.OriginalString))
            {
                DownloaderMethods.SaveTags(video.Path.OriginalString, finalTags);
            }
        }

		private void AddVideos(IEnumerable<StreamItemModel> videos)
        {
            if (videos == null) throw new ArgumentNullException(nameof(videos));
            if (!videos.Any()) return;

            ScanVideoCount++;
            try
            {
                lock (Videos)
                    Videos.AddRange(videos.Where(video => !Videos.Any(v => video.Video.ID == v.Video.ID)));

                foreach (var video in Videos)
                    video.ProgressChangedEventHandler += OnProgressChanged;

                Resolutions = new ObservableCollection<string>(DownloaderMethods.GetResolutions(Videos));
                OnPropertyChanged(nameof(Resolutions));
                OnPropertyChanged(nameof(ResolutionsAvailable));

                //TODO Filter videos without QualityLabels
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
            catch (InvalidOperationException)
            {
                Debug.WriteLine("InvalidOperation triggered");
            }
            finally
            {
                ScanVideoCount--;
            }
        }

        private async Task<IEnumerable<StreamItemModel>> GetVideosAsync(IEnumerable<string> urls, CancellationToken cToken = default)
        {
            List<StreamItemModel> items = new();

            List<Task> tasks = new();
            SemaphoreSlim queue = new(20, 20);
            foreach (var url in urls)
            {
                await queue.WaitAsync(cToken);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var video = await DownloaderMethods.downloadClient.RunVideoDataFetch(url);
                        StreamItemModel item = new(video);
                        item.Video.Url = url;

                        lock (items)
                            items.Add(item);
                    }
                    catch (Exception ex) { Debug.WriteLine(ex.Message); }
                }, cToken).ContinueWith(o => queue.Release()));
            }

            await Task.WhenAll(tasks);
            return items;
        }

        private static bool IsYoutubePlaylist(string url)
        {
            if (url.IsNullOrWhiteSpace()) return false;
            return url.Contains("youtu") && url.Contains("list=");
        }

        private async Task<bool> AskForPlaylistAsync()
        {
            return await dialogService.ShowInteractionDialogAsync("askForPlaylistDownloadTitle".GetLocalized(), "askForPlaylistDownloadContent".GetLocalized(), "playlist".GetLocalized(), "video".GetLocalized(), null) == true;
        }

        private void OnProgressChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(ResolutionsAvailable));
            OnPropertyChanged(nameof(DownloadProgress));

            if (canceledAll || DownloadProgress == 100)
            {
                taskbarProgressService.UpdateProgress(typeof(YouTubeDownloaderViewModel), 0);
                taskbarProgressService.UpdateState(typeof(YouTubeDownloaderViewModel), ProgressBarState.None);
                return;
            }
            taskbarProgressService.UpdateProgress(typeof(YouTubeDownloaderViewModel), DownloadProgress);
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

        private bool canceledAll = false;
        private async Task DownloadVideosAsync(IList<StreamItemModel> videos, bool getMp4, string qualityLabel)
        {
            if (videos == null || !videos.Any())
                throw new ArgumentException("videos is null or empty.");

            string path = null;
            if (!AppSettings.Instance.UseFixedStoragePaths)
            {
                path = await dialogService.ShowFolderPickerDialogAsync(DirectoryLocation.Videos);
                if (path == null) return;
            }

            canceledAll = false;
            taskbarProgressService?.SetType(typeof(YouTubeDownloaderViewModel));
            VideoNotFound = false;
            uint finishedCount = 0;
            uint unauthorizedAccessExceptions = 0;
            uint directoryNotFoundExceptions = 0;
            uint notEnoughSpaceExceptions = 0;

            List<Task> tasks = new();
            SemaphoreSlim queue = new(AppSettings.Instance.SimultaneousOperationCount, AppSettings.Instance.SimultaneousOperationCount);
            StreamItemModel[] items = videos.ToArray();
            StreamItemModel loadedVideo = null;
            
            CanceledAll += (o, e) => canceledAll = true;
            items.Where(i => i != null && videos.Contains(i)).ForEach(i => i.SetProgressToDefault());
            items.ForEach(v => v.QualityLabel = qualityLabel);
            foreach (var video in items)
            {
                if (canceledAll || !videos.Contains(video) || video.DownloadState == DownloadState.IsCancelled) continue;
                await queue.WaitAsync();

                if (canceledAll || !videos.Contains(video) || video.DownloadState == DownloadState.IsCancelled) continue;

                video.FinishedEventHandler += (o, e) =>
                {
                    loadedVideo = (StreamItemModel)o;
                    finishedCount++;
                };

                tasks.Add(DownloaderMethods.DownloadStreamAsync(video, getMp4, path).ContinueWith(t =>
                {
                    queue.Release();
                    if (t.Exception?.InnerException == null) return;

                    switch (t.Exception?.InnerException)
                    {
                        default:
                            Debug.WriteLine("Exception occured while saving the file.");
                            break;

                        case UnauthorizedAccessException:
                            unauthorizedAccessExceptions++;
                            break;

                        case DirectoryNotFoundException:
                            directoryNotFoundExceptions++;
                            break;

                        case NotEnoughSpaceException:
                            notEnoughSpaceExceptions++;
                            break;
                    }
                }));
            }
            await Task.WhenAll(tasks);

            //Remove downloaded videos from list
            if (AppSettings.Instance.ClearListsAfterOperation)
                items.ForEach(v => videos.Remove(v), v => videos.Contains(v) && v.Success);

            Debug.WriteLine("Downloadtask is done.");

            foreach (var dir in Directory.GetDirectories(pathProvider.DownloaderTempdir))
            {
                try { Directory.Delete(dir, true); }
                catch { /* Dont crash if a directory cant be deleted */ }
            }

            try
            {
	            if (unauthorizedAccessExceptions + directoryNotFoundExceptions + notEnoughSpaceExceptions > 0)
	            {
		            taskbarProgressService?.UpdateState(typeof(YouTubeDownloaderViewModel), ProgressBarState.Error);
		            await GlobalResources.DisplayFileSaveErrorDialog(unauthorizedAccessExceptions,
			            directoryNotFoundExceptions, notEnoughSpaceExceptions);
	            }

	            taskbarProgressService?.UpdateState(typeof(YouTubeDownloaderViewModel), ProgressBarState.None);

	            if (!AppSettings.Instance.SendMessageAfterDownload)
	            {
		            return;
	            }

	            if (finishedCount == 1)
	            {
		            loadedVideo.ShowToast();
	            }
	            else if (finishedCount > 1)
	            {
		            IEnumerable<string> filenames = null;

		            if (items.Any(v => v?.Path != null && Path.GetDirectoryName(v.Path.OriginalString) ==
			                Path.GetDirectoryName(loadedVideo.Path.OriginalString)))
		            {
			            filenames = items
				            .Where(v => v?.Path != null && v.DownloadState is DownloadState.IsDone &&
				                        File.Exists(v.Path.OriginalString))
				            .Select(v => Path.GetFileName(v.Path.OriginalString));
		            }

		            toastNotificationService.SendDownloadsDoneNotification(loadedVideo.Path.OriginalString,
			            finishedCount, filenames);
	            }
            }
            finally
            {
	            if (!(canceledAll || items.All(v => v.DownloadState == DownloadState.IsCancelled)))
				{
					bool errors = items.Any(v => videos.Contains(v) && v.Failed);
					DownloadDone?.Invoke(this, errors);
				}
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

            searchProcesses++;
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
            finally { searchProcesses--; }
        }
        private int searchProcesses = 0;

        [ICommand]
        private void ClearResults()
        {
            if (!DownloaderMethods.VideoSearchCancelSource.IsCancellationRequested)
                DownloaderMethods.VideoSearchCancelSource.Cancel();

            VideoNotFound = false;
            if (!SearchResults.Any()) return;
            SearchResults.Clear();
            lastSearch = (string.Empty, new Collection<SearchItemModel>());
        }

        [ICommand]
        private void CancelAll()
        {
            Videos.ForEach(v => v?.RaiseCancel());
            CanceledAll?.Invoke(this, EventArgs.Empty);
        }
        public event EventHandler CanceledAll;

        [ICommand]
        private void RemoveAll()
        {
            CancelAll();
            Videos.Clear();
        }

        public int DownloadProgress
        {
            get
            {
                if (Videos.Any() && Videos.All(v => v.ProgressInfo.DownloadState == YoutubeDLSharp.DownloadState.Success))
                    return 100;

                int progress = 0;
                int videocount = Videos.Count;

                //Get progress from all videos
                foreach (var video in Videos)
                    progress += video.ProgressInfo.Progress;

                if (videocount == 0)
                    return 0;

                return progress / videocount;
            }
        }

        /// <summary>
        /// The number of videos that get scanned in the moment.
        /// </summary>
        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(ReadyToDownload))]
        private int scanVideoCount;

        public bool QueueIsEmpty => !Videos.Any();
        public bool QueueIsNotEmpty => Videos.Any() && !DownloadFileCommand.IsRunning;
        public bool ReadyToDownload => QueueIsNotEmpty && ScanVideoCount == 0;
        public bool AnyResults => SearchResults.Any();
        public bool AddingVideo => AddVideoCommand.IsRunning || AddSearchedVideo.IsRunning;
        private void UpdateProgressStateProperties()
        {
            OnPropertyChanged(nameof(AddingVideo));
            OnPropertyChanged(nameof(QueueIsEmpty));
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
