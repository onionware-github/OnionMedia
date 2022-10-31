/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OnionMedia.Core.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace OnionMedia.Uno.Services
{
    sealed class DialogService : IDialogService
    {
        public async Task<string> ShowFolderPickerDialogAsync(DirectoryLocation location = DirectoryLocation.Home)
        {
            FolderPicker picker = new();
            picker.SuggestedStartLocation = DirectoryLocationToPickerLocationId(location);
            picker.FileTypeFilter.Add("*");
            var result = await picker.PickSingleFolderAsync();
            return result?.Path;
        }

        public async Task<string> ShowSingleFilePickerDialogAsync(DirectoryLocation location = DirectoryLocation.Home)
        {
            FileOpenPicker picker = new();
            picker.SuggestedStartLocation = DirectoryLocationToPickerLocationId(location);
            picker.FileTypeFilter.Add("*");
            var result = await picker.PickSingleFileAsync();
            return result?.Path;
        }

        public async Task<string[]> ShowMultipleFilePickerDialogAsync(DirectoryLocation location = DirectoryLocation.Home)
        {
            FileOpenPicker picker = new();
            picker.SuggestedStartLocation = DirectoryLocationToPickerLocationId(location);
            picker.FileTypeFilter.Add("*");
            var result = await picker.PickMultipleFilesAsync();
            return result?.Select(f => f?.Path).ToArray();
        }

        public async Task<bool?> ShowDialogAsync(DialogTextOptions dialogTextOptions)
        {
            if (dialogTextOptions == null)
                throw new ArgumentNullException(nameof(dialogTextOptions));

            ContentDialog dlg = new()
            {
                XamlRoot = UIResources.XamlRoot,
                Title = dialogTextOptions.Title,
                Content = new ScrollViewer()
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = BuildDialogContent(dialogTextOptions)
                },
                PrimaryButtonText = dialogTextOptions.YesButtonText,
                SecondaryButtonText = dialogTextOptions.NoButtonText,
                CloseButtonText = dialogTextOptions.CloseButtonText
            };

            var result = await dlg.ShowAsync();
            return ContentDialogResultToBool(result);
        }

        public async Task ShowInfoDialogAsync(string title, string content, string closeButtonText)
        {
            await new ContentDialog()
            {
                XamlRoot = UIResources.XamlRoot,
                Title = title,
                CloseButtonText = closeButtonText,
                Content = new ScrollViewer()
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new TextBlock()
                    {
                        Text = content,
                        TextWrapping = TextWrapping.WrapWholeWords
                    }
                }
            }.ShowAsync();
        }

        public async Task<bool?> ShowInteractionDialogAsync(string title, string content, string yesButtonText, string noButtonText, string cancelButtonText)
        {
            ContentDialog dlg = new()
            {
                XamlRoot = UIResources.XamlRoot,
                Title = title,
                PrimaryButtonText = yesButtonText,
                SecondaryButtonText = noButtonText,
                CloseButtonText = cancelButtonText,
                Content = new ScrollViewer()
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto, 
                    Content = new TextBlock()
                    {
                        Text = content,
                        TextWrapping = TextWrapping.WrapWholeWords
                    }
                }
            };

            var result = await dlg.ShowAsync();
            return ContentDialogResultToBool(result);
        }


        private static TextBlock BuildDialogContent(DialogTextOptions dialogTextOptions)
        {
            if (dialogTextOptions == null)
                throw new ArgumentNullException(nameof(dialogTextOptions));

            TextBlock txtBlock = new();
            txtBlock.Text = dialogTextOptions.Content;
            txtBlock.TextWrapping = GetTextWrapping(dialogTextOptions.ContentTextWrapping);
            return txtBlock;
        }

        private static TextWrapping GetTextWrapping(TextWrapMode wrapMode) => wrapMode switch
        {
            TextWrapMode.NoWrap => TextWrapping.NoWrap,
            TextWrapMode.Wrap => TextWrapping.Wrap,
            TextWrapMode.WrapWholeWords => TextWrapping.WrapWholeWords,
            _ => throw new NotImplementedException()
        };

        private static PickerLocationId DirectoryLocationToPickerLocationId(DirectoryLocation location) => location switch
        {
            DirectoryLocation.Home => PickerLocationId.ComputerFolder,
            DirectoryLocation.Desktop => PickerLocationId.Desktop,
            DirectoryLocation.Pictures => PickerLocationId.PicturesLibrary,
            DirectoryLocation.Music => PickerLocationId.MusicLibrary,
            DirectoryLocation.Videos => PickerLocationId.VideosLibrary,
            DirectoryLocation.Documents => PickerLocationId.DocumentsLibrary,
            DirectoryLocation.Downloads => PickerLocationId.Downloads,
            DirectoryLocation.Homegroup => PickerLocationId.HomeGroup,
            _ => PickerLocationId.Unspecified
        };

        private static bool? ContentDialogResultToBool(ContentDialogResult result) => result switch
        {
            ContentDialogResult.None => null,
            ContentDialogResult.Primary => true,
            ContentDialogResult.Secondary => false,
            _ => throw new NotImplementedException()
        };
    }
}
