/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using OnionMedia.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using YoutubeDLSharp;

namespace OnionMedia.Core.Models
{
    public partial class ProgressModel : ObservableObject
    {
        private int progress;
        public int Progress
        {
            get
            {
                if (IsDone)
                    return 100;
                return progress;
            }
            set
            {
                if (value == 100)
                    IsDone = true;

                else if (value == 0 && !IsDone)
                    progress = 0;

                else if (!IsDone && progress < value)
                    progress = value;
                OnPropertyChanged();
            }
        }

        private bool isDone;
        public bool IsDone
        {
            get => isDone;
            set
            {
                isDone = value;
                //Set progress to 0 when the download was restarted.
                if (isDone != value && !value)
                    progress = 0;
                OnPropertyChanged();
            }
        }
        public bool IsCancelledOrFailed { get; set; }

        [ObservableProperty] private string downloadSpeed;
        
        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(State))]
        private DownloadState downloadState;
        
        public string TotalSize { get; set; }
        
        public string State
        {
            get
            {
                if (DownloadState == DownloadState.Error && !IsCancelledOrFailed)
                    return DownloadState.None.ToString().GetLocalized("ProgressStates");

                return DownloadState.ToString().GetLocalized("ProgressStates");
            }
        }
    }
}
