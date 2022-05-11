using CommunityToolkit.Mvvm.DependencyInjection;

using Microsoft.UI.Xaml.Controls;

using OnionMedia.ViewModels;

namespace OnionMedia.Views
{
    //Not implemented site
    public sealed partial class PlaylistsPage : Page
    {
        public PlaylistsViewModel ViewModel { get; }

        public PlaylistsPage()
        {
            ViewModel = Ioc.Default.GetService<PlaylistsViewModel>();
            InitializeComponent();
        }
    }
}
