using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode.Videos;

namespace OnionMedia.Core.Services
{
    public interface IDownloaderDialogService
    {
        /// <summary>
        /// Lets the user select videos from a collection.
        /// </summary>
        /// <param name="videosFromPlaylist">The videos to select from.</param>
        /// <returns>The selected videos, returns an empty array when the dialog gets canceled.</returns>
        Task<IEnumerable<IVideo>> ShowPlaylistSelectorDialogAsync(IEnumerable<IVideo> videosFromPlaylist);
    }
}
