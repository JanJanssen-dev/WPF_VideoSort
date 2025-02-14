using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.IO;
using System.Collections.ObjectModel;
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
        private readonly FileExtensionService _fileExtensionService;
        private readonly UndoManager _undoManager;

        [ObservableProperty]
        private bool canUndo;

        [ObservableProperty]
        private string sourceFolder = string.Empty;

        [ObservableProperty]
        private string destinationFolder = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> logMessages = [];

        [ObservableProperty]
        private double progressValue;

        [ObservableProperty]
        private bool isSorting;

        [ObservableProperty]
        private SortSettingsViewModel sortSettings;

        [ObservableProperty]
        private string statusText = string.Empty;

        public MainViewModel()
        {
            _undoManager = new UndoManager();
            _mediaService = new MediaService();
            _fileExtensionService = new FileExtensionService();
            sortSettings = new SortSettingsViewModel(_mediaService);
            logMessages = [];

            // UndoManager PropertyChanged Event abonnieren
            _undoManager.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(UndoManager.CanUndo))
                {
                    CanUndo = _undoManager.CanUndo;
                }
            };

            LoadSettings();
        }

        private void AddLogMessage(string message)
        {
            App.Current?.Dispatcher.Invoke(() => LogMessages.Add(message));
        }

        private void UpdateProgress(double value)
        {
            App.Current?.Dispatcher.Invoke(() => ProgressValue = value);
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

        [RelayCommand(CanExecute = nameof(CanSortFiles))]
        private async Task SortFilesAsync()
        {
            if (string.IsNullOrEmpty(SourceFolder) || string.IsNullOrEmpty(DestinationFolder))
            {
                AddLogMessage("Bitte wähle Quell- und Zielordner aus.");
                StatusText = "Bitte Quell- und Zielordner auswählen.";
                return;
            }

            SaveSettings();
            IsSorting = true;
            ProgressValue = 0;
            StatusText = "Sortierung wird gestartet...";
            AddLogMessage(StatusText);

            try
            {
                _undoManager.StartOperationGroup("Dateisortierung");

                var supportedExtensions = _fileExtensionService.GetAllSupportedExtensions();
                var files = await Task.Run(() =>
                    supportedExtensions
                        .SelectMany(ext => Directory.GetFiles(SourceFolder, $"*{ext}", SearchOption.AllDirectories))
                        .ToList()
                );

                var processedFiles = new HashSet<string>();
                var totalFiles = files.Count;
                var currentFile = 0;

                AddLogMessage($"Gefundene Dateien: {totalFiles}");

                foreach (var file in files)
                {
                    try
                    {
                        var sourceFile = file;
                        var fileCategory = _fileExtensionService.GetCategory(file);
                        var mediaDate = await Task.Run(() => GetMediaDate(file));
                        var customValues = SortSettings.GetCustomValues(file);

                        string targetFolder = Path.Combine(
                            DestinationFolder,
                            fileCategory.ToString(),
                            SortSettings.Settings.GetFolderPath(mediaDate, customValues)
                        );

                        string destinationFile = Path.Combine(targetFolder, Path.GetFileName(file));

                        if (SortSettings.Settings.EnableDuplicateCheck &&
                            await CheckForDuplicatesAsync(file, processedFiles))
                        {
                            continue;
                        }

                        Directory.CreateDirectory(targetFolder);
                        File.Move(sourceFile, destinationFile);

                        _undoManager.AddOperation(new FileOperation(sourceFile, destinationFile));

                        processedFiles.Add(destinationFile);
                        currentFile++;
                        UpdateProgress((double)currentFile / totalFiles * 100);
                        StatusText = $"{currentFile} von {totalFiles} Dateien sortiert";
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = $"Fehler bei {Path.GetFileName(file)}: {ex.Message}";
                        AddLogMessage(errorMessage);
                        System.Diagnostics.Debug.WriteLine(errorMessage);
                    }
                }

                _undoManager.CommitOperationGroup();
            }
            catch (Exception ex)
            {
                string criticalErrorMessage = $"Kritischer Fehler: {ex.Message}";
                AddLogMessage(criticalErrorMessage);
                StatusText = "Sortierung fehlgeschlagen.";
                System.Diagnostics.Debug.WriteLine(criticalErrorMessage);
            }
            finally
            {
                IsSorting = false;
                StatusText = "Sortierung abgeschlossen.";
                AddLogMessage(StatusText);
                CanUndo = _undoManager.CanUndo;
            }
        }

        private async Task<bool> CheckForDuplicatesAsync(string file, HashSet<string> processedFiles)
        {
            foreach (var processedFile in processedFiles)
            {
                if (await Task.Run(() => _mediaService.AreDuplicates(
                    file,
                    processedFile,
                    SortSettings.Settings.UseHashForDuplicates)))
                {
                    AddLogMessage($"Duplikat gefunden: {Path.GetFileName(file)}");
                    return true;
                }
            }
            return false;
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
                                if (dateTime.HasValue)
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

        private bool CanSortFiles => !IsSorting &&
                                   !string.IsNullOrEmpty(SourceFolder) &&
                                   !string.IsNullOrEmpty(DestinationFolder);

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
                            SourceFolder = sourceFolder?.ToString() ?? string.Empty;

                        if (settingsData.TryGetValue("LastDestinationFolder", out var destFolder))
                            DestinationFolder = destFolder?.ToString() ?? string.Empty;

                        if (settingsData.TryGetValue("SortSettings", out var sortSettings) && sortSettings != null)
                        {
                            var settings = JsonSerializer.Deserialize<SortSettings>(
                                sortSettings.ToString() ?? "{}");
                            if (settings != null)
                                SortSettings.Settings = settings;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLogMessage($"Fehler beim Laden der Einstellungen: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settingsData = new Dictionary<string, object>
                {
                    { "LastSourceFolder", SourceFolder },
                    { "LastDestinationFolder", DestinationFolder },
                    { "SortSettings", SortSettings.Settings }
                };

                var json = JsonSerializer.Serialize(settingsData);
                File.WriteAllText(SETTINGS_FILE, json);
            }
            catch (Exception ex)
            {
                AddLogMessage($"Fehler beim Speichern der Einstellungen: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task UndoLastOperationAsync()
        {
            if (!_undoManager.CanUndo) return;

            IsSorting = true;
            StatusText = "Mache Änderungen rückgängig...";
            AddLogMessage(StatusText);

            try
            {
                var (successful, failed) = await _undoManager.UndoLastOperationGroup();
                AddLogMessage($"Rückgängig gemacht: {successful} erfolgreich, {failed} fehlgeschlagen");
            }
            catch (Exception ex)
            {
                AddLogMessage($"Fehler beim Rückgängigmachen: {ex.Message}");
            }
            finally
            {
                IsSorting = false;
                StatusText = "Rückgängig machen abgeschlossen.";
                CanUndo = _undoManager.CanUndo;
            }
        }
    }
}