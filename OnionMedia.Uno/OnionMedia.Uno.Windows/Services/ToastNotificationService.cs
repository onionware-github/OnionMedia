/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using CommunityToolkit.WinUI.Notifications;
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
        public void SendConversionDoneNotification(MediaItemModel mediafile, string filepath, string thumbnailpath)
        {
            new ToastContentBuilder()
                .AddText("conversionDone".GetLocalized("Resources"))
                .AddText(mediafile.Title)
                .AddText(mediafile.FileTags.Artist)
                .AddInlineImage(new Uri(thumbnailpath))
                .AddButton(new ToastButton()
                    .SetContent("playFile".GetLocalized("Resources"))
                    .AddArgument("action", "play")
                    .AddArgument("filepath", filepath)
                    .SetBackgroundActivation())
                .AddButton(new ToastButton()
                    .SetContent("openFolder".GetLocalized("Resources"))
                    .AddArgument("action", "open path")
                    .AddArgument("folderpath", filepath))
                .AddArgument("filenames", Path.GetFileName(filepath))
                .SetBackgroundActivation()
                .Show(toast =>
                {
                    toast.Group = "conversionMsgs";
                    toast.Tag = "0";
                });
        }

        public void SendConversionsDoneNotification(uint amount)
        {
            new ToastContentBuilder()
                .AddText("conversionDone".GetLocalized("Resources"))
                .AddText("filesConverted".GetLocalized("Resources").Replace("{0}", amount.ToString()))
                .Show(toast =>
                {
                    toast.Group = "conversionMsgs";
                    toast.Tag = "0";
                });
        }

        public void SendDownloadDoneNotification(VideoData video, string path)
        {
            new ToastContentBuilder()
                .AddText("downloadFinished".GetLocalized())
                .AddText(video.Title)
                .AddAttributionText(video.Uploader)
                .AddInlineImage(new Uri(video.Thumbnail))
                .AddButton(new ToastButton()
                    .SetContent("playFile".GetLocalized())
                    .AddArgument("action", "play")
                    .AddArgument("filepath", path)
                    .SetBackgroundActivation())
                .AddButton(new ToastButton()
                    .SetContent("openFolder".GetLocalized())
                    .AddArgument("action", "open path")
                    .AddArgument("folderpath", path))
                .AddArgument("filenames", Path.GetFileName(path))
                .SetBackgroundActivation()
                .Show(toast =>
                {
                    toast.Group = "downloadMsgs";
                    toast.Tag = "0";
                });
        }

        public void SendDownloadsDoneNotification(string folderpath, uint amount, IEnumerable<string> filenames = null)
        {
            var dialog = new ToastContentBuilder()
                .AddText("downloadFinished".GetLocalized())
                .AddText($"{amount} {"videosDownloaded".GetLocalized()}");

            if (filenames?.Any() == true)
            {
                StringBuilder files = new();
                foreach (var file in filenames)
                    files.AppendLine(file);

                dialog.AddButton(new ToastButton()
                    .SetContent("openFolder".GetLocalized())
                    .AddArgument("action", "open path")
                    .AddArgument("folderpath", folderpath)
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
}
