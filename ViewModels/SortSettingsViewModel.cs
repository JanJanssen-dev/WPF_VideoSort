using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using WPF_VideoSort.Models;
using WPF_VideoSort.Services;

namespace WPF_VideoSort.ViewModels
{
    public partial class SortSettingsViewModel : ObservableObject
    {
        private const string PATTERNS_FILE = "folder_patterns.json";
        private readonly MediaService _mediaService;

        [ObservableProperty]
        private SortSettings settings = new();

        [ObservableProperty]
        private ObservableCollection<FolderPattern> _savedPatterns = new();

        [ObservableProperty]
        private FolderPattern? _selectedPattern;

        [ObservableProperty]
        private string _newPatternName = string.Empty;

        [ObservableProperty]
        private string _newPatternValue = string.Empty;

        [ObservableProperty]
        private string _newPatternDescription = string.Empty;

        public List<SortOption> AvailableSortOptions => Enum.GetValues(typeof(SortOption))
                                                           .Cast<SortOption>()
                                                           .ToList();

        public SortSettingsViewModel(MediaService mediaService)
        {
            _mediaService = mediaService;
            InitializePatterns();
        }

        private void InitializePatterns()
        {
            var loadedPatterns = LoadPatternsFromFile();

            if (loadedPatterns.Count == 0)
            {
                loadedPatterns = CreateDefaultPatterns();
            }
            else
            {
                // Füge fehlende Standard-Patterns hinzu
                var defaultPatterns = CreateDefaultPatterns();
                var missingPatterns = defaultPatterns
                    .Where(dp => !loadedPatterns.Any(lp => lp.Name == dp.Name))
                    .ToList();

                loadedPatterns.AddRange(missingPatterns);
            }

            SavedPatterns = new ObservableCollection<FolderPattern>(loadedPatterns);
            SavePatternsToFile();
        }

        private List<FolderPattern> LoadPatternsFromFile()
        {
            try
            {
                if (File.Exists(PATTERNS_FILE))
                {
                    var json = File.ReadAllText(PATTERNS_FILE);
                    var patterns = JsonSerializer.Deserialize<List<FolderPattern>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return patterns ?? new List<FolderPattern>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Muster: {ex.Message}");
            }

            return new List<FolderPattern>();
        }

        private void SavePatternsToFile()
        {
            try
            {
                var json = JsonSerializer.Serialize(SavedPatterns.ToList());
                File.WriteAllText(PATTERNS_FILE, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Muster: {ex.Message}");
            }
        }

        private List<FolderPattern> CreateDefaultPatterns()
        {
            return new List<FolderPattern>
            {
                new("Standard Datum", "yyyy/MM",
                    "Sortiert nach Jahr und Monat (z.B. 2024/03)"),
                new("Ausführliches Datum", "yyyy/yyyy-MM",
                    "Jahr als Hauptordner, dann Jahr-Monat (z.B. 2024/2024-03)"),
                new("Mit Tag", "yyyy/MM/dd",
                    "Sortiert nach Jahr, Monat und Tag (z.B. 2024/03/15)"),
                new("Mit Standort", "{Location}/yyyy/MM",
                    "Sortiert nach Standort, dann nach Datum (z.B. Berlin/2024/03)"),
                new("Event-basiert", "yyyy/{Event}",
                    "Jahr als Hauptordner, dann Ereignisname (z.B. 2024/Urlaub_Italien)"),
                new("Kamera-Modell", "{CameraModel}/yyyy/MM",
                    "Sortiert nach Kameramodell, dann nach Datum (z.B. Canon_EOS_R5/2024/03)"),
                new("Datei-Typ", "{FileType}/yyyy/MM",
                    "Sortiert nach Dateityp (Fotos/Videos), dann nach Datum (z.B. Fotos/2024/03)"),
                new("Quartal", "yyyy/Q{Quarter}",
                    "Sortiert nach Jahr und Quartal (z.B. 2024/Q1)"),
                new("Projekt-basiert", "{Project}/yyyy-MM-dd",
                    "Projektordner mit Datum (z.B. Projekt_XY/2024-03-15)"),
                new("Jahr-Monat kombiniert", "yyyy-MM",
                    "Kombiniertes Jahr-Monats-Format (z.B. 2024-03)")
            };
        }

        [RelayCommand]
        private void SaveNewPattern()
        {
            if (string.IsNullOrWhiteSpace(NewPatternName) ||
                string.IsNullOrWhiteSpace(NewPatternValue))
                return;

            var pattern = new FolderPattern(
                NewPatternName.Trim(),
                NewPatternValue.Trim(),
                NewPatternDescription.Trim()
            );

            SavedPatterns.Add(pattern);
            SavePatternsToFile();

            // Felder zurücksetzen
            NewPatternName = string.Empty;
            NewPatternValue = string.Empty;
            NewPatternDescription = string.Empty;
        }

        [RelayCommand]
        private void DeletePattern(FolderPattern pattern)
        {
            if (pattern != null)
            {
                SavedPatterns.Remove(pattern);
                SavePatternsToFile();
            }
        }

        [RelayCommand]
        private void ApplyPattern(FolderPattern pattern)
        {
            if (pattern != null)
            {
                Settings.CustomPattern = pattern.Pattern;
                Settings.SortOption = SortOption.CustomPattern;

                OnPropertyChanged(nameof(Settings));
            }
        }

        public Dictionary<string, string> GetCustomValues(string filePath)
        {
            var values = new Dictionary<string, string>();

            if (Settings.UseGpsData)
            {
                var location = _mediaService.ExtractGpsLocation(filePath);
                if (location != null)
                {
                    values["Location"] = $"{location.Latitude:F2}_{location.Longitude:F2}";
                }
            }

            values["Event"] = "Unbekannt";
            values["Project"] = "Unbekannt";
            values["FileType"] = "Sonstiges";
            values["CameraModel"] = "Unbekannt";
            values["Quarter"] = ((DateTime.Now.Month - 1) / 3 + 1).ToString();

            return values;
        }
    }
}