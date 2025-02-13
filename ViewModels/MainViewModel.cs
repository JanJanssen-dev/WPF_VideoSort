using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Directory = System.IO.Directory;
using System.Text.Json;
using WPF_VideoSort.Models;
using WPF_VideoSort.Services;

namespace WPF_VideoSort.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private const string SETTINGS_FILE = "settings.json";
        private readonly MediaService _mediaService;
        private readonly SortSettingsViewModel _sortSettingsViewModel;

        [ObservableProperty]
        private string? sourceFolder;

        [ObservableProperty]
        private string? destinationFolder;

        [ObservableProperty]
        private ObservableCollection<string> logMessages = new();

        [ObservableProperty]
        private double progressValue;

        [ObservableProperty]
        private bool isSorting;

        public SortSettingsViewModel SortSettings => _sortSettingsViewModel;

        private readonly string[] supportedExtensions = new[]
        { 
            // Videos
            ".mp4", ".mts", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".m4v", ".3gp", ".webm", 
            // Bilder
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".heic", ".raw",
            ".cr2", ".nef", ".arw", ".dng"
        };

        public MainViewModel()
        {
            _mediaService = new MediaService();
            _sortSettingsViewModel = new SortSettingsViewModel(_mediaService);
            LoadSettings();
        }

        [RelayCommand]
        private void SelectSourceFolder()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                SourceFolder = dialog.FolderName;
                DestinationFolder = dialog.FolderName;
                SaveSettings();
            }
        }

        [RelayCommand]
        private void SelectDestinationFolder()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                DestinationFolder = dialog.FolderName;
                SaveSettings();
            }
        }

        [RelayCommand]
        private async Task SortVideosAsync()
        {
            if (string.IsNullOrEmpty(SourceFolder) || string.IsNullOrEmpty(DestinationFolder))
            {
                LogMessages.Add("Bitte wähle Quell- und Zielordner aus.");
                return;
            }

            SaveSettings();
            IsSorting = true;
            ProgressValue = 0;
            LogMessages.Add("Starte Sortierung...");

            try
            {
                await Task.Run(async () =>
                {
                    var files = supportedExtensions
                        .SelectMany(ext => Directory.GetFiles(SourceFolder, $"*{ext}"))
                        .ToList();

                    var processedFiles = new HashSet<string>(); // Für Duplikaterkennung
                    var totalFiles = files.Count;
                    var currentFile = 0;

                    foreach (var file in files)
                    {
                        try
                        {
                            // Überprüfe auf Duplikate falls aktiviert
                            if (SortSettings.Settings.EnableDuplicateCheck)
                            {
                                var isDuplicate = false;
                                foreach (var processedFile in processedFiles)
                                {
                                    if (_mediaService.AreDuplicates(file, processedFile,
                                        SortSettings.Settings.UseHashForDuplicates))
                                    {
                                        App.Current.Dispatcher.Invoke(() =>
                                            LogMessages.Add($"Duplikat gefunden: {Path.GetFileName(file)}"));
                                        isDuplicate = true;
                                        break;
                                    }
                                }
                                if (isDuplicate) continue;
                            }

                            var mediaDate = GetMediaDate(file);
                            var customValues = SortSettings.GetCustomValues(file);

                            string targetFolder = SortSettings.Settings.GetFolderPath(
                                mediaDate,
                                DestinationFolder,
                                customValues
                            );

                            Directory.CreateDirectory(targetFolder);
                            string destinationFile = Path.Combine(targetFolder, Path.GetFileName(file));

                            if (File.Exists(destinationFile))
                            {
                                App.Current.Dispatcher.Invoke(() =>
                                    LogMessages.Add($"Datei existiert bereits: {destinationFile}"));
                            }
                            else
                            {
                                File.Move(file, destinationFile);
                                processedFiles.Add(destinationFile);
                                App.Current.Dispatcher.Invoke(() =>
                                    LogMessages.Add($"Verschoben: {Path.GetFileName(file)} -> {destinationFile}"));
                            }

                            currentFile++;
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                ProgressValue = (double)currentFile / totalFiles * 100;
                            });
                        }
                        catch (Exception ex)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                                LogMessages.Add($"Fehler bei {Path.GetFileName(file)}: {ex.Message}"));
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                LogMessages.Add($"Kritischer Fehler: {ex.Message}");
            }
            finally
            {
                IsSorting = false;
                LogMessages.Add("Sortierung abgeschlossen.");
            }
        }

        private DateTime GetMediaDate(string filePath)
        {
            try
            {
                switch (SortSettings.Settings.SortOption)
                {
                    case SortOption.DateTaken:
                        var directories = ImageMetadataReader.ReadMetadata(filePath);
                        foreach (var directory in directories)
                        {
                            if (directory is ExifSubIfdDirectory exifSubIfd)
                            {
                                DateTime? dateTime = exifSubIfd.GetDateTime(ExifDirectoryBase.TagDateTimeOriginal);
                                if (dateTime != null)
                                    return dateTime.Value;
                            }
                        }
                        return File.GetCreationTime(filePath);

                    case SortOption.DateCreated:
                        return File.GetCreationTime(filePath);

                    case SortOption.DateModified:
                        return File.GetLastWriteTime(filePath);

                    case SortOption.FileCreationDate:
                        return File.GetCreationTime(filePath);

                    default:
                        return File.GetCreationTime(filePath);
                }
            }
            catch
            {
                return File.GetCreationTime(filePath);
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SETTINGS_FILE))
                {
                    var json = File.ReadAllText(SETTINGS_FILE);
                    var settingsData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (settingsData != null)
                    {
                        if (settingsData.TryGetValue("LastSourceFolder", out var sourceFolder))
                            SourceFolder = sourceFolder?.ToString();

                        if (settingsData.TryGetValue("LastDestinationFolder", out var destFolder))
                            DestinationFolder = destFolder?.ToString();

                        if (settingsData.TryGetValue("SortSettings", out var sortSettings))
                        {
                            var settings = JsonSerializer.Deserialize<SortSettings>(
                                sortSettings?.ToString() ?? "{}");
                            if (settings != null)
                                SortSettings.Settings = settings;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessages.Add($"Fehler beim Laden der Einstellungen: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settingsData = new Dictionary<string, object>
                {
                    { "LastSourceFolder", SourceFolder ?? "" },
                    { "LastDestinationFolder", DestinationFolder ?? "" },
                    { "SortSettings", SortSettings.Settings }
                };

                var json = JsonSerializer.Serialize(settingsData);
                File.WriteAllText(SETTINGS_FILE, json);
            }
            catch (Exception ex)
            {
                LogMessages.Add($"Fehler beim Speichern der Einstellungen: {ex.Message}");
            }
        }
    }
}