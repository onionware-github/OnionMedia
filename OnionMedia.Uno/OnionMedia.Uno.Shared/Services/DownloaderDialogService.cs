/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.UI.Xaml.Controls;
using OnionMedia.Core.Services;
using OnionMedia.Uno.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode.Videos;

namespace OnionMedia.Uno.Services
{
    sealed class DownloaderDialogService : IDownloaderDialogService
    {
        public async Task<IEnumerable<IVideo>> ShowPlaylistSelectorDialogAsync(IEnumerable<IVideo> videosFromPlaylist)
        {
            PlaylistSelectorDialog dlg = new(videosFromPlaylist) { XamlRoot = UIResources.XamlRoot };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return Array.Empty<IVideo>();
            return dlg.ViewModel.Videos.Where(v => v.IsSelected);
        }
    }
}
