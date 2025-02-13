using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WPF_VideoSort.Models;
using WPF_VideoSort.Services;

namespace WPF_VideoSort.ViewModels
{
    public partial class SortSettingsViewModel : ObservableObject
    {
        private readonly MediaService _mediaService;

        [ObservableProperty]
        private SortSettings settings = new();

        [ObservableProperty]
        private ObservableCollection<string> customPatternExamples = new()
        {
            "yyyy/MM",
            "yyyy/MM/dd",
            "yyyy/MM/dd/HH",
            "{Location}/yyyy/MM",
            "yyyy/{Event}/MM"
        };

        public List<SortOption> AvailableSortOptions => Enum.GetValues(typeof(SortOption))
                                                           .Cast<SortOption>()
                                                           .ToList();

        public SortSettingsViewModel(MediaService mediaService)
        {
            _mediaService = mediaService;
        }

        [RelayCommand]
        private void ApplyCustomPattern(string pattern)
        {
            Settings.CustomPattern = pattern;
        }

        public Dictionary<string, string> GetCustomValues(string filePath)
        {
            var values = new Dictionary<string, string>();

            // GPS-Standort extrahieren und in Location-Variable übersetzen
            if (Settings.UseGpsData)
            {
                var location = _mediaService.ExtractGpsLocation(filePath);
                if (location != null)
                {
                    values["Location"] = $"{location.Latitude:F2}_{location.Longitude:F2}";
                }
            }

            // Weitere benutzerdefinierte Werte können hier hinzugefügt werden
            values["Event"] = "Unbekannt";  // Beispiel

            return values;
        }
    }
}