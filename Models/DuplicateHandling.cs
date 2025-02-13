using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_VideoSort.Models
{
    public enum DuplicateHandling
    {
        Skip,   // Überspringen von Duplikaten
        Rename, // Umbenennen von Duplikaten
        Replace // Überschreiben von Duplikaten
    }
}
