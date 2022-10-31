/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using OnionMedia.Core.Extensions;
using OnionMedia.Core.Models;
using OnionMedia.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YoutubeDLSharp.Metadata;

namespace OnionMedia.Uno.Services
{
    sealed class ToastNotificationService : IToastNotificationService
    {
        void IToastNotificationService.SendConversionDoneNotification(MediaItemModel mediafile, string filepath, string thumbnailpath)
        {
            throw new NotImplementedException();
        }

        void IToastNotificationService.SendConversionsDoneNotification(uint amount)
        {
            throw new NotImplementedException();
        }

        void IToastNotificationService.SendDownloadDoneNotification(VideoData video, string path)
        {
            throw new NotImplementedException();
        }

        void IToastNotificationService.SendDownloadsDoneNotification(string folderpath, uint amount, IEnumerable<string> filenames)
        {
            throw new NotImplementedException();
        }
    }
}
