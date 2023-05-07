using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Db2Source.Converter
{
    public class RefButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null || value is DBNull || (value is string && (string)value == string.Empty)) ? Visibility.Collapsed : Visibility.Visible;
            //return (cell.DataContext == null) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
