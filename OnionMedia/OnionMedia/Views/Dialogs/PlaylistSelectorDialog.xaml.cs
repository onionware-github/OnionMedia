using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using OnionMedia.ViewModels.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using YoutubeExplode.Videos;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OnionMedia.Views.Dialogs
{
    public sealed partial class PlaylistSelectorDialog : ContentDialog
    {
        public PlaylistSelectorViewModel ViewModel { get; }

        public PlaylistSelectorDialog(IEnumerable<IVideo> videos)
        {
            ViewModel = new PlaylistSelectorViewModel(videos);
            this.InitializeComponent();
        }
    }
}
