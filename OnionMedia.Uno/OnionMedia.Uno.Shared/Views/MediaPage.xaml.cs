using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using OnionMedia.Core.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using OnionMedia.Core;
using OnionMedia.Core.Models;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace OnionMedia.Uno.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MediaPage : Page
    {
        public MediaViewModel ViewModel { get; }

        #region StaticReferences
        FFmpegCodecConfig FFmpegCodecs => GlobalResources.FFmpegCodecs;

        BitrateFormatter BitrateFormatter => BitrateFormatter.Instance;

        //Reference to the first ConversionPreset (customPreset)
        ConversionPreset CustomConversionPreset => ViewModel.ConversionPresets[0];

        string GetFilenameWithoutExtension() => Path.GetFileNameWithoutExtension(ViewModel.SelectedItem?.MediaFile.FileInfo.Name);
        #endregion

        public MediaPage()
        {
            ViewModel = IoC.Default.GetService<MediaViewModel>();
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
            this.Loaded += OnLoaded;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UIResources.XamlRoot = ((MediaPage)sender).XamlRoot;

            //Workaround for a bug from WinUI 3 (freezing ProgressRing)
            btnProgressRing.IsActive = false;
            btnProgressRing.IsActive = true;
        }

        private void Resolutionpreset_ItemClick(object sender, ItemClickEventArgs e) => resolutionFlyout.Hide();
        private void Frameratepreset_ItemClick(object sender, ItemClickEventArgs e) => framerateFlyout.Hide();
        private void SmallRemoveAll_Clicked(object sender, RoutedEventArgs e) => smallRemoveBtnFlyout.Hide();
        private void WideRemoveAll_Clicked(object sender, RoutedEventArgs e) => wideRemoveBtnFlyout.Hide();


        //Always set value to true on click.
        private void ToggleButton_Click(object sender, RoutedEventArgs e) => ((ToggleButton)sender).IsChecked = true;

        private async void mediaList_Drop(object sender, DragEventArgs e)
        {
            if (ViewModel.AddFileCommand.IsRunning || ViewModel.StartConversionCommand.IsRunning || !e.DataView.Contains(StandardDataFormats.StorageItems))
                return;

            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count == 0) return;
            await ViewModel.AddFilesAsync(items.Select(i => i.Path));
        }

        private void mediaList_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = (ViewModel.AddFileCommand.IsRunning || ViewModel.StartConversionCommand.IsRunning) ? DataPackageOperation.None : DataPackageOperation.Link;
        }
    }
}
