/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OnionMedia.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace OnionMedia.Services
{
    internal class DialogService : IDialogService
    {

        public async Task<string> ShowFolderPickerDialogAsync()
        {
            FolderPicker picker = new();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
            var result = await picker.PickSingleFolderAsync();
            return result?.Path;
        }

        public async Task<string> ShowSingleFilePickerDialogAsync()
        {
            FileOpenPicker picker = new();
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
            var result = await picker.PickSingleFileAsync();
            return result?.Path;
        }

        public async Task<string[]> ShowMultipleFilePickerDialogAsync()
        {
            FileOpenPicker picker = new();
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
            var result = await picker.PickMultipleFilesAsync();
            return result?.Select(f => f?.Path).ToArray();
        }

        public async Task<bool?> ShowDialogAsync(DialogTextOptions dialogTextOptions)
        {
            if (dialogTextOptions == null)
                throw new ArgumentNullException(nameof(dialogTextOptions));

            ContentDialog dlg = new()
            {
                XamlRoot = GlobalResources.XamlRoot,
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
                XamlRoot = GlobalResources.XamlRoot,
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
                XamlRoot = GlobalResources.XamlRoot,
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

        private static bool? ContentDialogResultToBool(ContentDialogResult result) => result switch
        {
            ContentDialogResult.None => null,
            ContentDialogResult.Primary => true,
            ContentDialogResult.Secondary => false,
            _ => throw new NotImplementedException()
        };
    }
}
