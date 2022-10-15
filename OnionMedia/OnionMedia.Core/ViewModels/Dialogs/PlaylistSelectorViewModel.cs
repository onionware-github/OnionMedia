/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using OnionMedia.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;

namespace OnionMedia.Core.ViewModels.Dialogs
{
    public class PlaylistSelectorViewModel : ObservableObject
    {
        public PlaylistSelectorViewModel(IEnumerable<IVideo> videos)
        {
            if (videos == null)
                throw new ArgumentNullException(nameof(videos));
            if (!videos.Any(v => v != null))
                throw new ArgumentException("Parameter videos does not contain valid videos.", nameof(videos));

            Videos = new List<SelectableVideo>();
            foreach (var video in videos.Where(v => v != null))
            {
                Videos.Add(new SelectableVideo(video));
                Videos[^1].PropertyChanged += (o, e) => UpdateSelectedVideos();
            }
        }

        public IList<SelectableVideo> Videos { get; }

        public bool AnySelectedVideos => Videos.Any(v => v.IsSelected);
        public int AmountOfSelectedVideos => Videos.Count(v => v.IsSelected);

        public bool? SelectionState
        {
            get => selectionState;
            set
            {
                SetProperty(ref selectionState, value);
                if (value != null)
                {
                    Videos.ForEach(v => v.IsSelected = (bool)value);
                    return;
                }
            }
        }
        private bool? selectionState = true;

        private bool? GetSelectionState()
        {
            if (Videos.All(v => v.IsSelected))
                return true;

            if (Videos.Any(v => v.IsSelected))
                return null;
            return false;
        }

        private void UpdateSelectedVideos()
        {
            SelectionState = GetSelectionState();
            OnPropertyChanged(nameof(AnySelectedVideos));
            OnPropertyChanged(nameof(AmountOfSelectedVideos));
        }
    }

    [INotifyPropertyChanged]
    public partial class SelectableVideo : IVideo
    {
        public SelectableVideo() { }

        public SelectableVideo(IVideo video)
        {
            if (video == null)
                throw new ArgumentNullException(nameof(video));

            Id = video.Id;
            Url = video.Url;
            Title = video.Title;
            Author = video.Author;
            Duration = video.Duration;
            Thumbnails = video.Thumbnails;

            if (Author?.Title != null)
                Uploader = Author.Title;
        }

        [JsonProperty("id")]
        public VideoId Id { get; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("uploader")]
        public string Uploader { get; set; }

        public Author Author { get; }

        [JsonProperty("duration")]
        public double? DurationAsFloatingNumber { get; set; }

        [JsonProperty("durationTimespan")]
        public TimeSpan? Duration { get; }

        public Thumbnail Thumbnail => Thumbnails?.GetWithHighestResolution();

        [JsonProperty("thumbnails")]
        public IReadOnlyList<Thumbnail> Thumbnails { get; set; }

        [ObservableProperty]
        private bool isSelected = true;
    }
}
