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

namespace WPF_VideoSort.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private const string SETTINGS_FILE = "settings.json";

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

        [ObservableProperty]
        private SortOption selectedSortOption = SortOption.DateTaken;

        public List<SortOption> AvailableSortOptions => Enum.GetValues(typeof(SortOption))
                                                           .Cast<SortOption>()
                                                           .ToList();

        public MainViewModel()
        {
            LoadSettings();
        }

        private readonly string[] supportedExtensions = new[]
        { 
            // Videos
            ".mp4", ".mts", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".m4v", ".3gp", ".webm", 
            // Bilder
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".heic", ".raw",
            ".cr2", ".nef", ".arw", ".dng"
        };

        [RelayCommand]
        private void HandleFolderDrop(object parameter)
        {
            if (parameter is string[] files && files.Length > 0 && Directory.Exists(files[0]))
            {
                SourceFolder = files[0];
                DestinationFolder = files[0];
                SaveSettings();
            }
        }

        [RelayCommand]
        private void HandleDestinationFolderDrop(object parameter)
        {
            if (parameter is string[] files && files.Length > 0 && Directory.Exists(files[0]))
            {
                DestinationFolder = files[0];
                SaveSettings();
            }
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
                await Task.Run(() =>
                {
                    var files = supportedExtensions
                        .SelectMany(ext => Directory.GetFiles(SourceFolder, $"*{ext}"))
                        .ToList();

                    var totalFiles = files.Count;
                    var processedFiles = 0;

                    foreach (var file in files)
                    {
                        try
                        {
                            var mediaDate = GetMediaDate(file);
                            string yearFolder = Path.Combine(DestinationFolder, mediaDate.Year.ToString());
                            string monthFolder = Path.Combine(yearFolder, mediaDate.ToString("yyyy-MM"));

                            Directory.CreateDirectory(monthFolder);

                            string destinationFile = Path.Combine(monthFolder, Path.GetFileName(file));

                            if (File.Exists(destinationFile))
                            {
                                App.Current.Dispatcher.Invoke(() =>
                                    LogMessages.Add($"Datei existiert bereits: {destinationFile}"));
                            }
                            else
                            {
                                File.Move(file, destinationFile);
                                App.Current.Dispatcher.Invoke(() =>
                                    LogMessages.Add($"Verschoben: {Path.GetFileName(file)} -> {destinationFile}"));
                            }

                            processedFiles++;
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                ProgressValue = (double)processedFiles / totalFiles * 100;
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
                switch (SelectedSortOption)
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
                    var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(SETTINGS_FILE));
                    if (settings != null)
                    {
                        SourceFolder = settings.LastSourceFolder;
                        DestinationFolder = settings.LastDestinationFolder;
                        SelectedSortOption = settings.LastSortOption;
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
                var settings = new Settings
                {
                    LastSourceFolder = SourceFolder,
                    LastDestinationFolder = DestinationFolder,
                    LastSortOption = SelectedSortOption
                };

                File.WriteAllText(SETTINGS_FILE, JsonSerializer.Serialize(settings));
            }
            catch (Exception ex)
            {
                LogMessages.Add($"Fehler beim Speichern der Einstellungen: {ex.Message}");
            }
        }
    }
}