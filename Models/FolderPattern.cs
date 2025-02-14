using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

namespace WPF_VideoSort.Models
{
    public partial class FolderPattern : ObservableObject
    {
        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string pattern = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        public FolderPattern()
        {
            name = string.Empty;
            pattern = string.Empty;
            description = string.Empty;
        }

        public FolderPattern(string name, string pattern, string description)
        {
            this.name = name;
            this.pattern = pattern;
            this.description = description;
        }
    }
}
