using Microsoft.UI.Xaml.Controls;
using OnionMedia.Core.Services;
using OnionMedia.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode.Videos;

namespace OnionMedia.Services
{
    internal class DownloaderDialogService : IDownloaderDialogService
    {
        public async Task<IEnumerable<IVideo>> ShowPlaylistSelectorDialogAsync(IEnumerable<IVideo> videosFromPlaylist)
        {
            PlaylistSelectorDialog dlg = new(videosFromPlaylist) { XamlRoot = GlobalResources.XamlRoot };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return Array.Empty<IVideo>();
            return dlg.ViewModel.Videos.Where(v => v.IsSelected);
        }
    }
}
