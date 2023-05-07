using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Db2Source
{
    public class VisibilityToBooleanConverter : IValueConverter
    {
        public VisibilityToBooleanConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((value != null) && (bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
