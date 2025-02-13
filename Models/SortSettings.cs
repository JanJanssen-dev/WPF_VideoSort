using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace WPF_VideoSort.Models
{
    public partial class SortSettings : ObservableObject
    {
        [ObservableProperty]
        private SortOption _sortOption = SortOption.DateCreated;

        [ObservableProperty]
        private string _customPattern = "yyyy/MM";

        partial void OnSortOptionChanged(SortOption value)
        {
            System.Diagnostics.Debug.WriteLine($"SortOption changed to: {value}");
        }

        partial void OnCustomPatternChanged(string value)
        {
            System.Diagnostics.Debug.WriteLine($"CustomPattern changed to: {value}");
        }

        [ObservableProperty]
        private bool _enableDuplicateCheck;

        [ObservableProperty]
        private bool _useHashForDuplicates;

        [ObservableProperty]
        private bool _useGpsData;

        [ObservableProperty]
        private int _gpsClusterRadius = 1000; // Meter

        [ObservableProperty]
        private DuplicateHandling _handleDuplicates = DuplicateHandling.Rename;

        public string GetFolderPath(
    DateTime date,
    Dictionary<string, string>? customValues = null)
        {
            string path = SortOption switch
            {
                SortOption.CustomPattern => CustomPattern,
                _ => "yyyy/MM"
            };

            // Datumsersetzung
            path = date.ToString(path);

            // Benutzerdefinierte Platzhalter ersetzen
            if (customValues != null)
            {
                foreach (var kvp in customValues)
                {
                    path = path.Replace($"{{{kvp.Key}}}", kvp.Value);
                }
            }

            return path;
        }
    }

    public enum SortOption
    {
        DateTaken,
        DateCreated,
        DateModified,
        FileCreationDate,
        GPSLocation,
        CustomPattern
    }

    public class GpsLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public double CalculateDistance(GpsLocation other)
        {
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