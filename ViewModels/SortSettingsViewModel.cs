
//TODO irgendwas komisch mit den Patterns, Undo funktion wieder rein nehmen 
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
        private SortSettings _settings = new();

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
            LoadSavedPatterns();
            AddMissingDefaultPatterns();
        }

        private void AddMissingDefaultPatterns()
        {
            // Liste der Standard-Musternamen
            var standardPatternNames = new[]
            {
                "Standard Datum",
                "Ausführliches Datum",
                "Mit Tag",
                "Mit Standort",
                "Event-basiert",
                "Kamera-Modell",
                "Datei-Typ",
                "Quartal",
                "Projekt-basiert",
                "Jahr-Monat kombiniert"
            };

            // Prüfe, welche Standard-Muster fehlen
            var missingPatterns = standardPatternNames
                .Where(name => !SavedPatterns.Any(p => p.Name == name))
                .ToList();

            // Füge fehlende Muster hinzu
            foreach (var missingPatternName in missingPatterns)
            {
                var missingPattern = GetDefaultPatternByName(missingPatternName);
                if (missingPattern != null)
                {
                    SavedPatterns.Add(missingPattern);
                }
            }

            // Speichere die aktualisierten Muster
            SavePatterns();
        }

        private FolderPattern? GetDefaultPatternByName(string name)
        {
            return name switch
            {
                "Standard Datum" => new FolderPattern(
                    "Standard Datum",
                    "yyyy/MM",
                    "Sortiert nach Jahr und Monat (z.B. 2024/03)"),
                "Ausführliches Datum" => new FolderPattern(
                    "Ausführliches Datum",
                    "yyyy/yyyy-MM",
                    "Jahr als Hauptordner, dann Jahr-Monat (z.B. 2024/2024-03)"),
                "Mit Tag" => new FolderPattern(
                    "Mit Tag",
                    "yyyy/MM/dd",
                    "Sortiert nach Jahr, Monat und Tag (z.B. 2024/03/15)"),
                "Mit Standort" => new FolderPattern(
                    "Mit Standort",
                    "{Location}/yyyy/MM",
                    "Sortiert nach Standort, dann nach Datum (z.B. Berlin/2024/03)"),
                "Event-basiert" => new FolderPattern(
                    "Event-basiert",
                    "yyyy/{Event}",
                    "Jahr als Hauptordner, dann Ereignisname (z.B. 2024/Urlaub_Italien)"),
                "Kamera-Modell" => new FolderPattern(
                    "Kamera-Modell",
                    "{CameraModel}/yyyy/MM",
                    "Sortiert nach Kameramodell, dann nach Datum (z.B. Canon_EOS_R5/2024/03)"),
                "Datei-Typ" => new FolderPattern(
                    "Datei-Typ",
                    "{FileType}/yyyy/MM",
                    "Sortiert nach Dateityp (Fotos/Videos), dann nach Datum (z.B. Fotos/2024/03)"),
                "Quartal" => new FolderPattern(
                    "Quartal",
                    "yyyy/Q{Quarter}",
                    "Sortiert nach Jahr und Quartal (z.B. 2024/Q1)"),
                "Projekt-basiert" => new FolderPattern(
                    "Projekt-basiert",
                    "{Project}/yyyy-MM-dd",
                    "Projektordner mit Datum (z.B. Projekt_XY/2024-03-15)"),
                "Jahr-Monat kombiniert" => new FolderPattern(
                    "Jahr-Monat kombiniert",
                    "yyyy-MM",
                    "Kombiniertes Jahr-Monats-Format (z.B. 2024-03)"),
                _ => null
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
            SavePatterns();

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
                SavePatterns();
            }
        }

        [RelayCommand]
        private void ApplyPattern(FolderPattern pattern)
        {
            if (pattern != null)
            {
                System.Diagnostics.Debug.WriteLine($"Applying pattern: {pattern.Name}");
                System.Diagnostics.Debug.WriteLine($"Pattern value: {pattern.Pattern}");

                // Setzen Sie den benutzerdefinierten Pfad
                Settings.CustomPattern = pattern.Pattern;

                // Explizit auf CustomPattern setzen
                Settings.SortOption = SortOption.CustomPattern;

                // Explizite Benachrichtigungen über Änderungen
                OnPropertyChanged(nameof(Settings));
                OnPropertyChanged(nameof(Settings.CustomPattern));
                OnPropertyChanged(nameof(Settings.SortOption));

                System.Diagnostics.Debug.WriteLine($"Current CustomPattern: {Settings.CustomPattern}");
                System.Diagnostics.Debug.WriteLine($"Current SortOption: {Settings.SortOption}");
            }
        }

        //[RelayCommand]
        //private void ApplyPattern(FolderPattern pattern)
        //{
        //    if (pattern != null)
        //    {
        //        Settings.CustomPattern = pattern.Pattern;
        //        Settings.SortOption = SortOption.CustomPattern;
        //    }
        //}

        private void LoadSavedPatterns()
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

                    if (patterns != null && patterns.Any())
                    {
                        SavedPatterns = new ObservableCollection<FolderPattern>(patterns);
                    }
                    else
                    {
                        AddDefaultPatterns();
                    }
                }
                else
                {
                    AddDefaultPatterns();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Muster: {ex.Message}");
                AddDefaultPatterns();
            }
        }

        private void AddDefaultPatterns()
        {
            SavedPatterns.Add(new FolderPattern(
                "Standard Datum",
                "yyyy/MM",
                "Sortiert nach Jahr und Monat (z.B. 2024/03)"));

            SavedPatterns.Add(new FolderPattern(
                "Ausführliches Datum",
                "yyyy/yyyy-MM",
                "Jahr als Hauptordner, dann Jahr-Monat (z.B. 2024/2024-03)"));

            SavedPatterns.Add(new FolderPattern(
                "Mit Tag",
                "yyyy/MM/dd",
                "Sortiert nach Jahr, Monat und Tag (z.B. 2024/03/15)"));

            SavedPatterns.Add(new FolderPattern(
                "Mit Standort",
                "{Location}/yyyy/MM",
                "Sortiert nach Standort, dann nach Datum (z.B. Berlin/2024/03)"));

            SavedPatterns.Add(new FolderPattern(
                "Event-basiert",
                "yyyy/{Event}",
                "Jahr als Hauptordner, dann Ereignisname (z.B. 2024/Urlaub_Italien)"));

            SavedPatterns.Add(new FolderPattern(
                "Kamera-Modell",
                "{CameraModel}/yyyy/MM",
                "Sortiert nach Kameramodell, dann nach Datum (z.B. Canon_EOS_R5/2024/03)"));

            SavedPatterns.Add(new FolderPattern(
                "Datei-Typ",
                "{FileType}/yyyy/MM",
                "Sortiert nach Dateityp (Fotos/Videos), dann nach Datum (z.B. Fotos/2024/03)"));

            SavedPatterns.Add(new FolderPattern(
                "Quartal",
                "yyyy/Q{Quarter}",
                "Sortiert nach Jahr und Quartal (z.B. 2024/Q1)"));

            SavedPatterns.Add(new FolderPattern(
                "Projekt-basiert",
                "{Project}/yyyy-MM-dd",
                "Projektordner mit Datum (z.B. Projekt_XY/2024-03-15)"));

            SavedPatterns.Add(new FolderPattern(
                "Jahr-Monat kombiniert",
                "yyyy-MM",
                "Kombiniertes Jahr-Monats-Format (z.B. 2024-03)"));

            SavePatterns();
        }

        private void SavePatterns()
        {
            try
            {
                var json = JsonSerializer.Serialize(SavedPatterns.ToList());
                File.WriteAllText(PATTERNS_FILE, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Muster: {ex.Message}");
                // Fehler beim Speichern - könnte hier einen Event auslösen
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

            return values;
        }
    }
}