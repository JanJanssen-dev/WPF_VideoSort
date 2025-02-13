using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.IO;
using System.Security.Cryptography;
using WPF_VideoSort.Models;
using Directory = System.IO.Directory;

namespace WPF_VideoSort.Services
{
    public class MediaService
    {
        public async Task<string> CalculateFileHashAsync(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = await md5.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public GpsLocation? ExtractGpsLocation(string filePath)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();

                if (gpsDirectory != null)
                {
                    var location = gpsDirectory.GetGeoLocation();
                    if (location != null)
                    {
                        return new GpsLocation
                        {
                            Latitude = location.Latitude,
                            Longitude = location.Longitude
                        };
                    }
                }
            }
            catch
            {
                // GPS-Daten konnten nicht gelesen werden
            }

            return null;
        }

        public bool AreDuplicates(string file1, string file2, bool useHash)
        {
            if (useHash)
            {
                var hash1 = CalculateFileHashAsync(file1).Result;
                var hash2 = CalculateFileHashAsync(file2).Result;
                return hash1 == hash2;
            }

            // Vergleiche Dateigröße und EXIF-Daten
            var fileInfo1 = new FileInfo(file1);
            var fileInfo2 = new FileInfo(file2);

            if (fileInfo1.Length != fileInfo2.Length)
                return false;

            try
            {
                var metadata1 = ImageMetadataReader.ReadMetadata(file1);
                var metadata2 = ImageMetadataReader.ReadMetadata(file2);

                var exif1 = metadata1.OfType<ExifIfd0Directory>().FirstOrDefault();
                var exif2 = metadata2.OfType<ExifIfd0Directory>().FirstOrDefault();

                if (exif1 != null && exif2 != null)
                {
                    // Vergleiche relevante EXIF-Tags
                    return CompareExifTags(exif1, exif2);
                }
            }
            catch
            {
                // Bei Fehlern beim EXIF-Vergleich nur Dateigröße berücksichtigen
                return true;
            }

            return true;
        }

        private bool CompareExifTags(ExifIfd0Directory exif1, ExifIfd0Directory exif2)
        {
            var relevantTags = new[]
            {
                ExifDirectoryBase.TagDateTime,
                ExifDirectoryBase.TagMake,
                ExifDirectoryBase.TagModel
            };

            foreach (var tag in relevantTags)
            {
                var value1 = exif1.GetString(tag);
                var value2 = exif2.GetString(tag);

                if (value1 != value2)
                    return false;
            }

            return true;
        }
    }
}