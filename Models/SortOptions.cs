using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_VideoSort.Models
{
    public enum SortOption
    {
        DateTaken,
        DateCreated,
        DateModified,
        FileCreationDate,
        GPSLocation,
        CustomPattern
    }

    public partial class SortSettings : ObservableObject
    {
        [ObservableProperty]
        private SortOption sortOption;

        [ObservableProperty]
        private string customPattern = "yyyy/MM";

        [ObservableProperty]
        private bool enableDuplicateCheck;

        [ObservableProperty]
        private bool useHashForDuplicates;

        [ObservableProperty]
        private bool useGpsData;

        [ObservableProperty]
        private int gpsClusterRadius = 1000; // Meter

        public string GetFolderPath(DateTime date, string baseFolder, Dictionary<string, string>? customValues = null)
        {
            if (SortOption == SortOption.CustomPattern)
            {
                var path = CustomPattern;

                // Ersetze Datums-Platzhalter
                path = date.ToString(path);

                // Ersetze benutzerdefinierte Platzhalter
                if (customValues != null)
                {
                    foreach (var kvp in customValues)
                    {
                        path = path.Replace($"{{{kvp.Key}}}", kvp.Value);
                    }
                }

                return Path.Combine(baseFolder, path);
            }

            // Standard-Datumsbasierte Sortierung
            return Path.Combine(baseFolder, date.Year.ToString(), date.ToString("yyyy-MM"));
        }
    }

    public class GpsLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public double CalculateDistance(GpsLocation other)
        {
            // Haversine-Formel für Entfernungsberechnung
            const double R = 6371e3; // Erdradius in Metern
            var φ1 = Latitude * Math.PI / 180;
            var φ2 = other.Latitude * Math.PI / 180;
            var Δφ = (other.Latitude - Latitude) * Math.PI / 180;
            var Δλ = (other.Longitude - Longitude) * Math.PI / 180;

            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }
    }
}