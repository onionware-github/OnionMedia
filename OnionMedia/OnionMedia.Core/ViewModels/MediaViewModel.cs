/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
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
using OnionMedia.Core;
using OnionMedia.Core.Classes;
using OnionMedia.Core.Enums;
using OnionMedia.Core.Extensions;
using OnionMedia.Core.Models;
using OnionMedia.Core.Services;

namespace OnionMedia.Core.ViewModels
{
    [ObservableObject]
    public sealed partial class MediaViewModel
    {
        public MediaViewModel(IDialogService dialogService, IDispatcherService dispatcher, IConversionPresetDialog conversionPresetDialog, IFiletagEditorDialog filetagEditorDialog, IToastNotificationService toastNotificationService, ITaskbarProgressService taskbarProgressService)
        {
            this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            this.conversionPresetDialog = conversionPresetDialog ?? throw new ArgumentNullException(nameof(conversionPresetDialog));
            this.filetagEditorDialog = filetagEditorDialog ?? throw new ArgumentNullException(nameof(filetagEditorDialog));
            this.toastNotificationService = toastNotificationService ?? throw new ArgumentNullException(nameof(toastNotificationService));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.taskbarProgressService = taskbarProgressService;

            if (this.taskbarProgressService != null)
                PropertyChanged += (o, e) =>
                {
                    if (e.PropertyName != nameof(Progress)) return;
                    if (allCanceled || Progress == 100)
                    {
                        this.taskbarProgressService.UpdateProgress(typeof(MediaViewModel), 0);
                        this.taskbarProgressService.UpdateState(typeof(MediaViewModel), ProgressBarState.None);
                        return;
                    }
                    this.taskbarProgressService.UpdateProgress(typeof(MediaViewModel), (float)Progress);
                };

            InsertConversionPresetsFromJson();

            //Sort presets
            var sorted = ConversionPresets.OrderByDescending(p => p.VideoAvailable).ThenBy(p => p.Name);
            ConversionPresets = new(sorted);

            //Insert the "Custom Preset"
            ConversionPresets.Insert(0, new ConversionPreset("custom".GetLocalized(resources)));
            ConversionPresets.CollectionChanged += UpdateConversionPresets;
            if (ConversionPresets.Count > 1)
                SelectedConversionPreset = ConversionPresets[1];
            else
                SelectedConversionPreset = ConversionPresets[0];

            Files.CollectionChanged += Files_CollectionChanged;
            AddFileCommand = new(async () => await AddFilesAsync());
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
                OnPropertyChanged(nameof(CanEditTags));
            };
            EditTagsCommand = new(EditTagsAsync);
            SetFramerateCommand = new(fps =>
            {
                if (SelectedItem == null) return;
                SelectedItem.FPS = fps;
                OnPropertyChanged(nameof(SelectedItem));
            });
        }

        private readonly IDialogService dialogService;
        private readonly IConversionPresetDialog conversionPresetDialog;
        private readonly IFiletagEditorDialog filetagEditorDialog;
        private readonly IToastNotificationService toastNotificationService;
        private readonly IDispatcherService dispatcher;
        private readonly ITaskbarProgressService taskbarProgressService;

        private static readonly IPathProvider pathProvider = IoC.Default.GetService<IPathProvider>() ?? throw new ArgumentNullException();

        private void Files_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(FilesAreEmpty));
            OnPropertyChanged(nameof(CanExecuteConversion));
        }

        private void InsertConversionPresetsFromJson()
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
                    ConversionPresets.AddRange(JsonSerializer.Deserialize<IEnumerable<ConversionPreset>>(File.ReadAllText(Path.Combine(pathProvider.InstallPath, "Data", "ConversionPresets.json"))));
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

        public static readonly double[] KHzSampleRates = { 0, 22.05, 32, 44.1, 48, 88.2 };

        public AsyncRelayCommand AddFileCommand { get; }
        public RelayCommand<MediaItemModel> RemoveFileCommand { get; }
        public AsyncRelayCommand StartConversionCommand { get; }
        public AsyncRelayCommand AddConversionPresetCommand { get; }
        public AsyncRelayCommand<ConversionPreset> EditConversionPresetCommand { get; }
        public AsyncRelayCommand<ConversionPreset> DeleteConversionPresetCommand { get; }
        public AsyncRelayCommand EditTagsCommand { get; }
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
                OnPropertyChanged(nameof(CanEditTags));

                if (value?.MediaInfo.PrimaryVideoStream is not null)
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
        public bool CanEditTags => ItemSelected && SelectedItem.FileTagsAvailable && !StartConversionCommand.IsRunning;

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

        /// <summary>
        /// Adds the specified files to the list. If <paramref name="filepaths"/> is null or have no content, a FilePicker appears.
        /// </summary>
        /// <param name="filepaths">The files to add to the list.</param>
        public async Task AddFilesAsync(IEnumerable<string> filepaths = null)
        {
            string[] result = filepaths?.ToArray();
            if (result == null || result.Length == 0)
            {
                result = await dialogService.ShowMultipleFilePickerDialogAsync(DirectoryLocation.Videos);
                if (result == null || !result.Any()) return;
            }

            int failedCount = 0;
            AddingFiles = true;
            foreach (var filepath in result)
            {
                try
                {
                    if (Files.Any(f => f.MediaFile.FileInfo.FullName == filepath))
                        continue;

                    Files.Add(await MediaItemModel.CreateAsync(new FileInfo(filepath)));
                    Files[^1].Progress += async (o, e) => await dispatcher.EnqueueAsync(() => OnPropertyChanged(nameof(Progress)));
                    Files[^1].Cancel += async (o, e) => await dispatcher.EnqueueAsync(() => OnPropertyChanged(nameof(Progress)));
                    Files[^1].Error += (o, e) => Debug.WriteLine("Error while converting " + ((MediaItemModel)o).MediaFile.FileInfo.Name);
                    Files[^1].PropertyChanged += async (o, e) => await dispatcher.EnqueueAsync(() => OnPropertyChanged(nameof(VideoEnabled)));
                }
                catch { failedCount++; }
            }
            AddingFiles = false;
            if (failedCount > 0)
            {
                (string title, string content) dlgContent;
                if (result.Length == 1)
                    dlgContent = ("fileNotSupported".GetLocalized(dialogResources), "fileNotSupportedText".GetLocalized(dialogResources));
                else if (result.Length == failedCount)
                    dlgContent = ("filesNotSupported".GetLocalized(dialogResources), "filesNotSupportedText".GetLocalized(dialogResources));
                else
                    dlgContent = ("specificFilesNotSupported".GetLocalized(dialogResources), "specificFilesNotSupportedText".GetLocalized(dialogResources).Replace("{0}", failedCount.ToString()));

                await dialogService.ShowInfoDialogAsync(dlgContent.title, dlgContent.content, "OK");
            }
            if (Files.Any())
                SelectedItem = Files[^1];
        }

        private async Task AddConversionPresetAsync()
        {
            ConversionPreset newPreset = await conversionPresetDialog.ShowCustomPresetDialogAsync(ConversionPresets.Select(p => p.Name));
            if (newPreset == null) return;

            //Add the new preset and sort the presets (exclude the standard preset [0] from sorting)
            ConversionPresets.Add(newPreset);
            ReorderConversionPresets();
            SelectedConversionPreset = newPreset;
        }

        private async Task EditConversionPresetAsync(ConversionPreset conversionPreset)
        {
            if (conversionPreset == null)
                throw new ArgumentNullException(nameof(conversionPreset));

            if (!ConversionPresets.Contains(conversionPreset))
                throw new ArgumentException("ConversionPresets does not contain conversionPreset.");

            ConversionPreset editedPreset = await conversionPresetDialog.ShowCustomPresetDialogAsync(conversionPreset, ConversionPresets.Select(p => p.Name));
            if (editedPreset == null) return;

            //Rename the preset and sort the presets (exclude the standard preset [0] from sorting)
            ConversionPresets[ConversionPresets.IndexOf(conversionPreset)] = editedPreset;
            ReorderConversionPresets();
            SelectedConversionPreset = editedPreset;
        }

        private async Task DeleteConversionPresetAsync(ConversionPreset conversionPreset)
        {
            if (conversionPreset == null)
                throw new ArgumentNullException(nameof(conversionPreset));

            if (!ConversionPresets.Contains(conversionPreset))
                throw new ArgumentException("ConversionPresets does not contain conversionPreset.");

            bool deletePreset = await dialogService.ShowInteractionDialogAsync("title".GetLocalized(deletePresetDialog),
                                                                             "content".GetLocalized(deletePresetDialog).Replace("{0}", conversionPreset.Name),
                                                                             "delete".GetLocalized(deletePresetDialog),
                                                                             "cancel".GetLocalized(deletePresetDialog),
                                                                             null) ?? false;
            if (!deletePreset) return;

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

            string path = null;
            if (!AppSettings.Instance.UseFixedStoragePaths)
            {
                //TODO: Check if this path is writable.
                path = await dialogService.ShowFolderPickerDialogAsync(DirectoryLocation.Videos);
                if (path == null) return;
            }

            allCanceled = false;
            taskbarProgressService?.SetType(typeof(MediaViewModel));
            var files = new List<MediaItemModel>(Files);
            var queue = new SemaphoreSlim(AppSettings.Instance.SimultaneousOperationCount, AppSettings.Instance.SimultaneousOperationCount);
            List<Task> tasks = new();
            files.ForEach(f => f.Uncancel());
            string targetDir;

            uint completed = 0;
            uint unauthorizedAccessExceptions = 0;
            uint directoryNotFoundExceptions = 0;
            uint notEnoughSpaceExceptions = 0;
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
                if (AppSettings.Instance.UseFixedStoragePaths && ((!SelectedConversionPreset.VideoAvailable && !file.UseCustomOptions) || !file.CustomOptions.VideoAvailable && file.UseCustomOptions))
                    targetDir = AppSettings.Instance.ConvertedAudioSavePath;
                else if (AppSettings.Instance.UseFixedStoragePaths)
                    targetDir = AppSettings.Instance.ConvertedVideoSavePath;
                else
                    targetDir = path;

                string outputPath = Path.ChangeExtension(Path.Combine(targetDir, file.MediaFile.FileInfo.Name), file.UseCustomOptions ? file.CustomOptions.Format.Name : SelectedConversionPreset.Format.Name);
                file.Complete += (o, e) =>
                {
                    completed++;
                    lastCompleted = (MediaItemModel)o;
                    if (File.Exists(e?.Output?.Name))
                        lastCompletedPath = e.Output.Name;
                };
                tasks.Add(file.ConvertFileAsync(Path.Combine(targetDir, file.MediaFile.FileInfo.Name), SelectedConversionPreset).ContinueWith(t =>
                {
                    queue.Release();
                    if (t?.Exception?.InnerException == null) return;

                    switch (t.Exception?.InnerException)
                    {
                        default:
                            Debug.WriteLine("Exception occured while saving the file.");
                            break;

                        case UnauthorizedAccessException:
                            unauthorizedAccessExceptions++;
                            break;

                        case DirectoryNotFoundException:
                            directoryNotFoundExceptions++;
                            break;

                        case NotEnoughSpaceException:
                            notEnoughSpaceExceptions++;
                            break;
                    }
                }));
            }
            await Task.WhenAll(tasks);

            //Remove converted files from list.
            if (AppSettings.Instance.ClearListsAfterOperation)
                files.ForEach(f => Files.Remove(f), f => Files.Contains(f) && f.ConversionState is FFmpegConversionState.Done);

            Debug.WriteLine("Conversion done");

            try
            {
                foreach (var dir in Directory.GetDirectories(pathProvider.ConverterTempdir))
                {
                    try { Directory.Delete(dir, true); }
                    catch { /* Dont crash if a directory cant be deleted */ }
                }
            }
            catch {Console.WriteLine("Failed to get temporary conversion folders.");}
            
            if (unauthorizedAccessExceptions + directoryNotFoundExceptions + notEnoughSpaceExceptions > 0)
            {
                taskbarProgressService?.UpdateState(typeof(MediaViewModel), ProgressBarState.Error);
                await GlobalResources.DisplayFileSaveErrorDialog(unauthorizedAccessExceptions, directoryNotFoundExceptions, notEnoughSpaceExceptions);
            }

            taskbarProgressService?.UpdateState(typeof(MediaViewModel), ProgressBarState.None);
            if (!AppSettings.Instance.SendMessageAfterConversion) return;
            if (completed == 1)
            {
                await lastCompleted.ShowToastAsync(lastCompletedPath);
            }
            else if (completed > 1)
            {
                toastNotificationService.SendConversionsDoneNotification(completed);
            }
        }

        [ICommand]
        public void SetResolution(Resolution res)
        {
            if (SelectedItem is null) return;
            SelectedItem.Width = res.Width;
            SelectedItem.Height = res.Height;
            OnPropertyChanged(nameof(SelectedItem));
        }

        private async Task EditTagsAsync()
        {
            TagsEditedFlyoutIsOpen = false;
            if (SelectedItem == null || !SelectedItem.FileTagsAvailable) return;

            FileTags newTags = await filetagEditorDialog.ShowTagEditorDialogAsync(SelectedItem.FileTags);
            if (newTags == null) return;
            try
            {
                bool result = SelectedItem.ApplyNewTags(newTags);
                if (!result)
                {
                    await dialogService.ShowInfoDialogAsync("error".GetLocalized(resources), "tagerrormsg".GetLocalized(resources), "OK");
                    return;
                }

                //Show TagsEditedTip for 4 seconds
                TagsEditedFlyoutIsOpen = true;
                await Task.Delay(4000);
                TagsEditedFlyoutIsOpen = false;
            }
            catch (FileNotFoundException)
            {
                await dialogService.ShowInfoDialogAsync("fileNotFound".GetLocalized(resources), "fileNotFoundText".GetLocalized(resources), "OK");
                Files.Remove(SelectedItem);
            }
            catch
            {
                await dialogService.ShowInfoDialogAsync("error".GetLocalized(resources), "changesErrorMsg".GetLocalized(resources), "OK");
            }
        }

        private bool allCanceled = false;
        [ICommand]
        private void CancelAll()
        {
            Files.Where(f => f.ConversionState is FFmpegConversionState.None or FFmpegConversionState.Converting or FFmpegConversionState.Moving)
                .OrderBy(f => f.ConversionState)
                .ForEach(f => f.RaiseCancel());
            allCanceled = true;
        }

        [ICommand]
        private void RemoveAll()
        {
            CancelAll();
            Files.Clear();
        }

        private readonly string conversionPresetsPath = Path.Combine(pathProvider.LocalPath, "Media", "ConversionPresets.json");
        private const string resources = "Resources";
        private const string dialogResources = "DialogResources";
        private const string deletePresetDialog = "DeletePresetDialog";
    }
}
