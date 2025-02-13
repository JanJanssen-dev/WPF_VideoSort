using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_VideoSort.Models
{
    public class Settings
    {
        public string? LastSourceFolder { get; set; }
        public string? LastDestinationFolder { get; set; }
        public SortOption LastSortOption { get; set; }
    }
}