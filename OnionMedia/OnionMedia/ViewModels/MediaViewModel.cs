﻿/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 * This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OnionMedia.Core.Classes;
using OnionMedia.Core.Enums;
using OnionMedia.Core.Extensions;
using OnionMedia.Core.Models;
using OnionMedia.Views.Dialogs;
using Windows.Storage.Pickers;
using WinRT.Interop;
using CommunityToolkit.WinUI.Notifications;

namespace OnionMedia.ViewModels
{
    [ObservableObject]
    public sealed partial class MediaViewModel
    {
        public MediaViewModel()
        {
            try
            {
                //Try to read the .json file that contains the presets.
                ConversionPresets.AddRange(JsonSerializer.Deserialize<IEnumerable<ConversionPreset>>(File.ReadAllText(conversionPresetsPath)));
            }
            catch (Exception)
            {
                try
                {
                    //If the file is missing or corrupted, try to read the supplied file.
                    ConversionPresets.AddRange(JsonSerializer.Deserialize<IEnumerable<ConversionPreset>>(File.ReadAllText(GlobalResources.Installpath + @"\Data\ConversionPresets.json")));
                }
                catch (Exception) { } //Dont crash when the supplied .json file is missing or corrupted too.
                finally
                {
                    //Finally create a new .json file for the presets.
                    if (!Directory.Exists(Path.GetDirectoryName(conversionPresetsPath)))
                        Directory.CreateDirectory(Path.GetDirectoryName(conversionPresetsPath));
                    using var sr = File.CreateText(conversionPresetsPath);
                    sr.Write(JsonSerializer.Serialize<IEnumerable<ConversionPreset>>(ConversionPresets));
                }
            }

            //Sort presets
            var sorted = ConversionPresets.OrderByDescending(p => p.VideoAvailable).ThenBy(p => p.Name);
            ConversionPresets = new(sorted);

            ConversionPresets.Insert(0, new ConversionPreset("custom".GetLocalized(resources)));
            ConversionPresets.CollectionChanged += UpdateConversionPresets;
            if (ConversionPresets.Count > 1)
                SelectedConversionPreset = ConversionPresets[1];
            else
                SelectedConversionPreset = ConversionPresets[0];

            Files.CollectionChanged += Files_CollectionChanged;
            AddFileCommand = new(AddFileAsync);
            RemoveFileCommand = new(f =>
            {
                f.RaiseCancel();
                Files.Remove(f);
                if (Files.Any())
                    SelectedItem = Files[^1];
            }, f => f != null);
            AddConversionPresetCommand = new(AddConversionPresetAsync);
            EditConversionPresetCommand = new(EditConversionPresetAsync, preset => preset != null && ConversionPresets.Contains(preset) && preset != ConversionPresets[0]);
            DeleteConversionPresetCommand = new(DeleteConversionPresetAsync, preset => preset != null && ConversionPresets.Contains(preset) && preset != ConversionPresets[0]);
            StartConversionCommand = new(ConvertFilesAsync);
            StartConversionCommand.PropertyChanged += (o, e) =>
            {
                OnPropertyChanged(nameof(CanExecuteConversion));
                OnPropertyChanged(nameof(ItemSelectedAndIdle));
            };
            EditTagsCommand = new(EditTagsAsync);
            SetResolutionCommand = new(res =>
            {
                if (SelectedItem == null) return;
                SelectedItem.Width = res.Width;
                SelectedItem.Height = res.Height;
                OnPropertyChanged(nameof(SelectedItem));
            });
            SetFramerateCommand = new(fps =>
            {
                if (SelectedItem == null) return;
                SelectedItem.FPS = fps;
                OnPropertyChanged(nameof(SelectedItem));
            });
        }

        private void Files_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(FilesAreEmpty));
            OnPropertyChanged(nameof(CanExecuteConversion));
        }

        private async void UpdateConversionPresets(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!File.Exists(conversionPresetsPath))
            {
                if (!Directory.Exists(Path.GetDirectoryName(conversionPresetsPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(conversionPresetsPath));
                File.Create(conversionPresetsPath);
            }

            List<ConversionPreset> presets = new(((IEnumerable<ConversionPreset>)sender).Where(p => p != null));
            if (presets.Any())
                await File.WriteAllTextAsync(conversionPresetsPath, JsonSerializer.Serialize<IEnumerable<ConversionPreset>>(presets.GetRange(1, presets.Count - 1)));
            else
                await File.WriteAllTextAsync(conversionPresetsPath, "[]");

            if (SelectedConversionPreset == null && ConversionPresets.Any())
                SelectedConversionPreset = ConversionPresets[0];
        }

        public AsyncRelayCommand AddFileCommand { get; }
        public RelayCommand<MediaItemModel> RemoveFileCommand { get; }
        public AsyncRelayCommand StartConversionCommand { get; }
        public AsyncRelayCommand AddConversionPresetCommand { get; }
        public AsyncRelayCommand<ConversionPreset> EditConversionPresetCommand { get; }
        public AsyncRelayCommand<ConversionPreset> DeleteConversionPresetCommand { get; }
        public AsyncRelayCommand EditTagsCommand { get; }
        public RelayCommand<Resolution> SetResolutionCommand { get; }
        public RelayCommand<double> SetFramerateCommand { get; }

        public ObservableCollection<MediaItemModel> Files { get; } = new();
        public ObservableCollection<ConversionPreset> ConversionPresets { get; private set; } = new();
        public ObservableCollection<Resolution> Resolutions { get; } = new()
        {
            defaultResolution,
            new Resolution("NTSC DV", 720, 480),
            new Resolution("NTSC", 720, 486),
            new Resolution("PAL", 720, 576),
            new Resolution("HD", 1280, 720),
            new Resolution("Full HD", 1920, 1080),
            new Resolution("QHD", 2560, 1440),
            new Resolution("4K", 3840, 2160),
            new Resolution("8K", 7680, 4320)
        };
        private static readonly Resolution defaultResolution = new("source".GetLocalized(resources), 0, 0);

        public MediaItemModel SelectedItem
        {
            get => selectedItem;
            set
            {
                if (!SetProperty(ref selectedItem, value)) return;
                OnPropertyChanged(nameof(SelectedConversionPreset));
                OnPropertyChanged(nameof(ItemSelected));
                OnPropertyChanged(nameof(ItemSelectedAndIdle));
                OnPropertyChanged(nameof(VideoEnabled));

                if (value != null && value.MediaInfo.PrimaryVideoStream != null)
                    Resolutions[0] = new Resolution("source".GetLocalized(resources), (uint)value.MediaInfo.PrimaryVideoStream.Width, (uint)value.MediaInfo.PrimaryVideoStream.Height);
                else
                    Resolutions[0] = defaultResolution;
            }
        }
        private MediaItemModel selectedItem;

        public bool VideoEnabled => ItemSelected && ((SelectedConversionPreset != null && SelectedConversionPreset.VideoAvailable && !SelectedItem.UseCustomOptions) || (SelectedItem.CustomOptions?.VideoAvailable == true && SelectedItem.UseCustomOptions));
        public bool ItemSelected => SelectedItem != null;
        public bool ItemSelectedAndIdle => ItemSelected && !StartConversionCommand.IsRunning;
        public bool FilesAreEmpty => !Files.Any();
        public bool CanExecuteConversion => !StartConversionCommand.IsRunning && !FilesAreEmpty && !AddingFiles;

        public ConversionPreset SelectedConversionPreset
        {
            get => selectedConversionPreset;
            set
            {
                if (value == null || !SetProperty(ref selectedConversionPreset, value)) return;
                OnPropertyChanged(nameof(IsDefaultPresetSelected));
                OnPropertyChanged(nameof(VideoEnabled));
            }
        }
        private ConversionPreset selectedConversionPreset;
        public bool IsDefaultPresetSelected => SelectedConversionPreset == ConversionPresets[0];

        public double Progress
        {
            get
            {
                if (!Files.Any())
                    return 0;

                double progress = 0;
                int maximumProgress = Files.Count * 100;
                foreach (var file in Files)
                    progress += file.ConversionProgress;
                return progress / maximumProgress * 100;
            }
        }

        [ObservableProperty]
        [AlsoNotifyChangeFor(nameof(CanExecuteConversion))]
        private bool addingFiles;

        [ObservableProperty]
        private bool tagsEditedFlyoutIsOpen;

        private void ReorderConversionPresets()
        {
            ConversionPresets.CollectionChanged -= UpdateConversionPresets;
            ConversionPresets = new ObservableCollection<ConversionPreset>(ConversionPresets.OrderByDescending(p => p == ConversionPresets[0]).ThenByDescending(p => p.VideoAvailable).ThenBy(p => p.Name));
            ConversionPresets.CollectionChanged += UpdateConversionPresets;
            OnPropertyChanged(nameof(ConversionPresets));
        }

        private async Task AddFileAsync()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));
            var result = await picker.PickMultipleFilesAsync();
            if (result == null || !result.Any()) return;

            int failedCount = 0;
            AddingFiles = true;
            foreach (var file in result)
            {
                try
                {
                    if (Files.Any(f => f.MediaFile.FileInfo.FullName == file.Path))
                        continue;

                    Files.Add(await MediaItemModel.CreateAsync(new FileInfo(file.Path)));
                    Files[^1].Progress += async (o, e) => await GlobalResources.DispatcherQueue.EnqueueAsync(() => OnPropertyChanged(nameof(Progress)));
                    Files[^1].Cancel += async (o, e) => await GlobalResources.DispatcherQueue.EnqueueAsync(() => OnPropertyChanged(nameof(Progress)));
                    Files[^1].Error += (o, e) => Debug.WriteLine("Error while converting " + ((MediaItemModel)o).MediaFile.FileInfo.Name);
                    Files[^1].PropertyChanged += async (o, e) => await GlobalResources.DispatcherQueue.EnqueueAsync(() => OnPropertyChanged(nameof(VideoEnabled)));
                }
                catch (FFMpegCore.Exceptions.FFMpegException) { failedCount++; }
            }
            AddingFiles = false;
            if (failedCount > 0)
            {
                (string title, string content) dlgContent;
                if (result.Count == 1)
                    dlgContent = ("fileNotSupported".GetLocalized(dialogResources), "fileNotSupportedText".GetLocalized(dialogResources));
                else if (result.Count == failedCount)
                    dlgContent = ("filesNotSupported".GetLocalized(dialogResources), "filesNotSupportedText".GetLocalized(dialogResources));
                else
                    dlgContent = ("specificFilesNotSupported".GetLocalized(dialogResources), "specificFilesNotSupportedText".GetLocalized(dialogResources).Replace("{0}", failedCount.ToString()));

                ContentDialog dlg = new()
                {
                    XamlRoot = GlobalResources.XamlRoot,
                    Title = dlgContent.title,
                    Content = dlgContent.content,
                    PrimaryButtonText = "OK"
                };
                await dlg.ShowAsync();
            }
            if (Files.Any())
                SelectedItem = Files[^1];
        }

        private async Task AddConversionPresetAsync()
        {
            ConversionPresetDialog dlg = new(ConversionPresets.Select(p => p.Name)) { XamlRoot = GlobalResources.XamlRoot };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            //Add the new preset and sort the presets (exclude the standard preset [0] from sorting)
            ConversionPresets.Add(dlg.ConversionPreset);
            ReorderConversionPresets();
            SelectedConversionPreset = dlg.ConversionPreset;
        }

        private async Task EditConversionPresetAsync(ConversionPreset conversionPreset)
        {
            if (conversionPreset == null)
                throw new ArgumentNullException(nameof(conversionPreset));

            if (!ConversionPresets.Contains(conversionPreset))
                throw new ArgumentException("ConversionPresets does not contain conversionPreset.");

            ConversionPresetDialog dlg = new(conversionPreset, ConversionPresets.Select(p => p.Name)) { XamlRoot = GlobalResources.XamlRoot };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            //Rename the preset and sort the presets (exclude the standard preset [0] from sorting)
            ConversionPresets[ConversionPresets.IndexOf(conversionPreset)] = dlg.ConversionPreset;
            ReorderConversionPresets();
            SelectedConversionPreset = dlg.ConversionPreset;
        }

        private async Task DeleteConversionPresetAsync(ConversionPreset conversionPreset)
        {
            if (conversionPreset == null)
                throw new ArgumentNullException(nameof(conversionPreset));

            if (!ConversionPresets.Contains(conversionPreset))
                throw new ArgumentException("ConversionPresets does not contain conversionPreset.");

            ContentDialog dlg = new()
            {
                Title = "title".GetLocalized(deletePresetDialog),
                XamlRoot = GlobalResources.XamlRoot,
                Content = new TextBlock()
                {
                    Text = "content".GetLocalized(deletePresetDialog).Replace("{0}", conversionPreset.Name),
                    TextWrapping = TextWrapping.WrapWholeWords
                },
                PrimaryButtonText = "delete".GetLocalized(deletePresetDialog),
                SecondaryButtonText = "cancel".GetLocalized(deletePresetDialog)
            };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

            ConversionPresets.Remove(conversionPreset);
            if (ConversionPresets.Count > 1)
                SelectedConversionPreset = ConversionPresets[1];
            else
                SelectedConversionPreset = ConversionPresets[0];
        }

        private async Task ConvertFilesAsync()
        {
            if (SelectedConversionPreset == null)
                throw new Exception("SelectedConversionPreset is null.");

            //Create the target directory if it does not exist.
            Directory.CreateDirectory(targetFolder);

            var files = new List<MediaItemModel>(Files);
            var queue = new SemaphoreSlim(AppSettings.Instance.SimultaneousOperationCount, AppSettings.Instance.SimultaneousOperationCount);
            List<Task> tasks = new();
            files.ForEach(f => f.Uncancel());
            string targetDir;

            int completed = 0;
            MediaItemModel lastCompleted = null;
            string lastCompletedPath = string.Empty;
            foreach (var file in files)
            {
                //Check whether the file still exists or has already been cancelled.
                //If the file doesnt exist anymore, skip this iteration.
                if (!Files.Contains(file) || file.ConversionState is FFmpegConversionState.Cancelled) continue;
                await queue.WaitAsync();

                //After the waiting time, check again whether the file was removed or canceled.
                if (!Files.Contains(file) || file.ConversionState is FFmpegConversionState.Cancelled) continue;

                //Put the video in ConvertedAudioSavePath, when no video stream exists, otherwise use the ConvertedVideoSavePath.
                if ((!SelectedConversionPreset.VideoAvailable && !file.UseCustomOptions) || !file.CustomOptions.VideoAvailable && file.UseCustomOptions)
                    targetDir = AppSettings.Instance.ConvertedAudioSavePath;
                else
                    targetDir = AppSettings.Instance.ConvertedVideoSavePath;

                string outputPath = Path.ChangeExtension(Path.Combine(targetDir, file.MediaFile.FileInfo.Name), file.UseCustomOptions ? file.CustomOptions.Format.Name : SelectedConversionPreset.Format.Name);
                file.Complete += (o, e) =>
                {
                    completed++;
                    lastCompleted = (MediaItemModel)o;
                    lastCompletedPath = outputPath;
                };
                tasks.Add(file.ConvertFileAsync(Path.Combine(targetDir, file.MediaFile.FileInfo.Name), SelectedConversionPreset).ContinueWith(t => queue.Release()));
            }
            await Task.WhenAll(tasks);
            Debug.WriteLine("Conversion done");

            foreach (var dir in Directory.GetDirectories(GlobalResources.ConverterTempdir))
            {
                try { Directory.Delete(dir, true); }
                catch { /* Dont crash if a directory cant be deleted */ }
            }
            if (!AppSettings.Instance.SendMessageAfterConversion) return;

            if (completed == 1)
            {
                await lastCompleted.ShowToastAsync(lastCompletedPath);
            }
            else if (completed > 1)
            {
                new ToastContentBuilder()
                            .AddText("conversionDone".GetLocalized("Resources"))
                            .AddText("filesConverted".GetLocalized("Resources").Replace("{0}", completed.ToString()))
                            .Show(toast =>
                            {
                                toast.Group = "conversionMsgs";
                                toast.Tag = "0";
                            });
            }
        }

        private async Task EditTagsAsync()
        {
            TagsEditedFlyoutIsOpen = false;
            if (SelectedItem == null || !SelectedItem.FileTagsAvailable) return;

            var dlg = new EditTagsDialog(SelectedItem.FileTags) { XamlRoot = GlobalResources.XamlRoot };
            if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
            try
            {
                bool result = SelectedItem.ApplyNewTags(dlg.FileTags);
                if (!result)
                {
                    _ = await new ContentDialog()
                    {
                        XamlRoot = GlobalResources.XamlRoot,
                        Title = "error".GetLocalized(resources),
                        Content = new TextBlock() { Text = "tagerrormsg".GetLocalized(resources) },
                        PrimaryButtonText = "OK"
                    }.ShowAsync();
                    return;
                }

                //Show TagsEditedTip for 4 seconds
                TagsEditedFlyoutIsOpen = true;
                await Task.Delay(4000);
                TagsEditedFlyoutIsOpen = false;
            }
            catch (FileNotFoundException)
            {
                _ = await new ContentDialog()
                {
                    XamlRoot = GlobalResources.XamlRoot,
                    Title = "fileNotFound".GetLocalized(resources),
                    Content = new TextBlock() { Text = "fileNotFoundText".GetLocalized(resources) },
                    PrimaryButtonText = "OK"
                }.ShowAsync();
                Files.Remove(SelectedItem);
            }
            catch
            {
                _ = await new ContentDialog()
                {
                    XamlRoot = GlobalResources.XamlRoot,
                    Title = "error".GetLocalized(resources),
                    Content = new TextBlock() { Text = "changesErrorMsg".GetLocalized(resources) },
                    PrimaryButtonText = "OK"
                }.ShowAsync();
            }
        }

        [ICommand]
        private void CancelAll() => Files.Where(f => f.ConversionState is FFmpegConversionState.None or FFmpegConversionState.Converting)
                                         .OrderBy(f => f.ConversionState)
                                         .ForEach(f => f.RaiseCancel());

        private readonly string conversionPresetsPath = GlobalResources.LocalPath + @"\Media\ConversionPresets.json";
        private readonly string targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "OnionMedia", "Converted");
        private const string resources = "Resources";
        private const string dialogResources = "DialogResources";
        private const string deletePresetDialog = "DeletePresetDialog";
    }
}