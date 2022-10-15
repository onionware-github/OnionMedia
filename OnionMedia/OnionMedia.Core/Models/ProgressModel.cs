/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using OnionMedia.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeDLSharp;

namespace OnionMedia.Core.Models
{
    public class ProgressModel
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
            }
        }
        public bool IsCancelledOrFailed { get; set; }

        public string DownloadSpeed { get; set; }
        public string TotalSize { get; set; }

        public DownloadState DownloadState { get; set; }
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
