using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WPF_VideoSort.Models;

namespace WPF_VideoSort.Converter
{
    public class SortOptionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SortOption currentOption)
            {
                return currentOption == SortOption.CustomPattern
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}