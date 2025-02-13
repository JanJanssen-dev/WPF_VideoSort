using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_VideoSort
{
    public enum SortOption
    {
        DateTaken,          // EXIF Aufnahmedatum
        DateCreated,        // Erstellungsdatum der Datei
        DateModified,       // Änderungsdatum der Datei
        FileCreationDate    // Dateisystem-Erstellungsdatum
    }
}