using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_VideoSort.Models
{
    public class SortProgress
    {
        public string SourceFolder { get; set; }
        public string DestinationFolder { get; set; }
        public List<string> ProcessedFiles { get; set; } = new();
        public List<string> PendingFiles { get; set; } = new();
        public SortOption SortOption { get; set; }
        public DateTime LastSaved { get; set; }
    }

    public partial class ProgressManager : ObservableObject
    {
        private const string PROGRESS_FILE = "sort_progress.json";

        [ObservableProperty]
        private bool hasStoredProgress;

        public void SaveProgress(string sourceFolder, string destinationFolder,
            List<string> processedFiles, List<string> pendingFiles, SortOption sortOption)
        {
            var progress = new SortProgress
            {
                SourceFolder = sourceFolder,
                DestinationFolder = destinationFolder,
                ProcessedFiles = processedFiles,
                PendingFiles = pendingFiles,
                SortOption = sortOption,
                LastSaved = DateTime.Now
            };

            File.WriteAllText(PROGRESS_FILE, JsonSerializer.Serialize(progress));
            HasStoredProgress = true;
        }

        public SortProgress LoadProgress()
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

        public void ClearProgress()
        {
            if (File.Exists(PROGRESS_FILE))
            {
                File.Delete(PROGRESS_FILE);
            }
            HasStoredProgress = false;
        }
    }
}