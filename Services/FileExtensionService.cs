using System.IO;
using WPF_VideoSort.Models;

public class FileExtensionService
{
    private readonly Dictionary<string, FileCategory> _extensionCategories;

    public FileExtensionService()
    {
        _extensionCategories = InitializeExtensionCategories();
    }

    public FileCategory GetCategory(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        return _extensionCategories.TryGetValue(extension, out FileCategory category)
            ? category
            : FileCategory.Other;
    }

    public IEnumerable<string> GetAllSupportedExtensions()
    {
        return _extensionCategories.Keys;
    }

    private Dictionary<string, FileCategory> InitializeExtensionCategories()
    {
        return new Dictionary<string, FileCategory>
        {
            // Video-Formate
            [".mp4"] = FileCategory.Video,
            [".mts"] = FileCategory.Video,
            [".mov"] = FileCategory.Video,
            [".avi"] = FileCategory.Video,
            [".mkv"] = FileCategory.Video,
            [".wmv"] = FileCategory.Video,
            [".flv"] = FileCategory.Video,
            [".m4v"] = FileCategory.Video,
            [".3gp"] = FileCategory.Video,
            [".webm"] = FileCategory.Video,
            [".mpg"] = FileCategory.Video,
            [".mpeg"] = FileCategory.Video,
            [".m2v"] = FileCategory.Video,
            [".h264"] = FileCategory.Video,
            [".vob"] = FileCategory.Video,
            [".mxf"] = FileCategory.Video,
            [".rm"] = FileCategory.Video,
            [".rmvb"] = FileCategory.Video,
            [".asf"] = FileCategory.Video,
            [".divx"] = FileCategory.Video,
            [".m2ts"] = FileCategory.Video,
            [".ts"] = FileCategory.Video,
            [".qt"] = FileCategory.Video,
            [".ogv"] = FileCategory.Video,
            [".f4v"] = FileCategory.Video,
            [".mp2"] = FileCategory.Video,
            [".mpe"] = FileCategory.Video,
            [".mpv"] = FileCategory.Video,
            [".m2p"] = FileCategory.Video,

            // Bild-Formate
            [".jpg"] = FileCategory.Image,
            [".jpeg"] = FileCategory.Image,
            [".png"] = FileCategory.Image,
            [".gif"] = FileCategory.Image,
            [".bmp"] = FileCategory.Image,
            [".tiff"] = FileCategory.Image,
            [".tif"] = FileCategory.Image,
            [".heic"] = FileCategory.Image,
            [".raw"] = FileCategory.Image,
            [".cr2"] = FileCategory.Image,
            [".nef"] = FileCategory.Image,
            [".arw"] = FileCategory.Image,
            [".dng"] = FileCategory.Image,
            [".webp"] = FileCategory.Image,
            [".svg"] = FileCategory.Image,
            [".psd"] = FileCategory.Image,
            [".ai"] = FileCategory.Image,
            // ... [Rest der Bildformate]

            // Audio-Formate
            [".mp3"] = FileCategory.Audio,
            [".wav"] = FileCategory.Audio,
            [".wma"] = FileCategory.Audio,
            [".aac"] = FileCategory.Audio,
            // ... [Rest der Audioformate]

            // Dokument-Formate
            [".pdf"] = FileCategory.Document,
            [".doc"] = FileCategory.Document,
            [".docx"] = FileCategory.Document,
            // ... [Rest der Dokumentformate]

            // Archiv-Formate
            [".zip"] = FileCategory.Archive,
            [".rar"] = FileCategory.Archive,
            [".7z"] = FileCategory.Archive,
            // ... [Rest der Archivformate]

            // 3D und CAD-Formate
            [".stl"] = FileCategory.ThreeDimensional,
            [".obj"] = FileCategory.ThreeDimensional,
            // ... [Rest der 3D-Formate]

            // Programmier- und Skript-Formate
            [".cs"] = FileCategory.Programming,
            [".vb"] = FileCategory.Programming,
            // ... [Rest der Programmierformate]

            // Spezielle Medien-Formate
            [".sub"] = FileCategory.SpecialMedia,
            [".srt"] = FileCategory.SpecialMedia,
            // ... [Rest der speziellen Medienformate]
        };
    }
}