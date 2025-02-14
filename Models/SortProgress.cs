using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WPF_VideoSort.Models
{
    // Diese Klasse könnte auch partial sein und ObservableObject nutzen
    public partial class SortProgress : ObservableObject
    {
        [ObservableProperty]
        private string sourceFolder = string.Empty;

        [ObservableProperty]
        private string destinationFolder = string.Empty;

        [ObservableProperty]
        private List<string> processedFiles = new();

        [ObservableProperty]
        private List<string> pendingFiles = new();

        [ObservableProperty]
        private SortOption sortOption;

        [ObservableProperty]
        private DateTime lastSaved;
    }

    public partial class ProgressManager : ObservableObject
    {
        private const string PROGRESS_FILE = "sort_progress.json";

        [ObservableProperty]
        private bool hasStoredProgress;

        // Statt einzelner Parameter verwenden wir ein Progress-Objekt
        public void SaveProgress(SortProgress progress)
        {
            try
            {
                File.WriteAllText(PROGRESS_FILE, JsonSerializer.Serialize(progress));
                HasStoredProgress = true;
            }
            catch
            {
                HasStoredProgress = false;
            }
        }

        public SortProgress? LoadProgress()
        {
            if (!File.Exists(PROGRESS_FILE))
            {
                HasStoredProgress = false;
                return null;
            }

            try
            {
                var progress = JsonSerializer.Deserialize<SortProgress>(
                    File.ReadAllText(PROGRESS_FILE));
                HasStoredProgress = progress != null;
                return progress;
            }
            catch
            {
                HasStoredProgress = false;
                return null;
            }
        }

        [RelayCommand]
        private void ClearProgress()
        {
            if (File.Exists(PROGRESS_FILE))
            {
                File.Delete(PROGRESS_FILE);
            }
            HasStoredProgress = false;
        }
    }
}