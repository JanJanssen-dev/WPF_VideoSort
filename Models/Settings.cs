using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_VideoSort.Models
{
    public partial class Settings : ObservableObject
    {
        [ObservableProperty]
        private string? lastSourceFolder;

        [ObservableProperty]
        private string? lastDestinationFolder;

        [ObservableProperty]
        private SortOption lastSortOption;

        public Settings()
        {
            lastSourceFolder = null;
            lastDestinationFolder = null;
            lastSortOption = SortOption.DateCreated; // Standardwert
        }
    }
}