using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

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

            LogMessages.Add("Starte Sortierung...");

            try
            {
                await Task.Run(() =>
                {
                    var files = Directory.GetFiles(SourceFolder, "*.mp4");
                    foreach (var file in files)
                    {
                        try
                        {
                            var creationTime = File.GetCreationTime(file);
                            string yearFolder = Path.Combine(DestinationFolder, creationTime.Year.ToString());
                            string monthFolder = Path.Combine(yearFolder, creationTime.ToString("yyyy-MM"));

                            Directory.CreateDirectory(monthFolder);

                            string destinationFile = Path.Combine(monthFolder, Path.GetFileName(file));

                            if (File.Exists(destinationFile))
                            {
                                App.Current.Dispatcher.Invoke(() =>
                                    LogMessages.Add($"Datei existiert bereits: {destinationFile}"));
                                continue;
                            }

                            File.Move(file, destinationFile);

                            App.Current.Dispatcher.Invoke(() =>
                                LogMessages.Add($"Verschoben: {Path.GetFileName(file)} -> {destinationFile}"));
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

            LogMessages.Add("Sortierung abgeschlossen.");
        }
    }
}