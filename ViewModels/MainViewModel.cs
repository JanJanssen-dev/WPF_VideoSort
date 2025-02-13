using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Directory = System.IO.Directory;

namespace WPF_VideoSort.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
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
        [ObservableProperty]
        private string? remainingTime;

        private DateTime? startTime;

        public List<SortOption> AvailableSortOptions => Enum.GetValues(typeof(SortOption))
                                                           .Cast<SortOption>()
                                                           .ToList();

        private readonly string[] supportedExtensions = new[]
        { 
            // Videos
            ".mp4",  // Standard Video Format
            ".mts",  // Sony/Panasonic Kamera Format
            ".mov",  // QuickTime/Apple Format
            ".avi",  // Windows Video Format
            ".mkv",  // Matroska Video Format
            ".wmv",  // Windows Media Video
            ".flv",  // Flash Video Format
            ".m4v",  // iPod/PSP Video Format
            ".3gp",  // Mobil-Video Format
            ".webm", // Web Video Format
            
            // Bilder
            ".jpg",  // Standard JPEG
            ".jpeg", // Standard JPEG (alternative Endung)
            ".png",  // Portable Network Graphics
            ".gif",  // Graphics Interchange Format
            ".bmp",  // Windows Bitmap
            ".tiff", // Tagged Image Format
            ".tif",  // Tagged Image Format (alternative Endung)
            ".heic", // High Efficiency Image Format (iOS)
            ".raw",  // Raw Kamera Format
            ".cr2",  // Canon Raw Format
            ".nef",  // Nikon Raw Format
            ".arw",  // Sony Raw Format
            ".dng"   // Digital Negative Format
        };

        [RelayCommand]
        private void SelectSourceFolder()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                SourceFolder = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void SelectDestinationFolder()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                DestinationFolder = dialog.FolderName;
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

            IsSorting = true;
            ProgressValue = 0;
            startTime = DateTime.Now; // Startzeit setzen
            RemainingTime = "Berechne...";
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
                        // Versuche EXIF-Daten zu lesen
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
                        // Fallback auf Erstellungsdatum
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
    }
}