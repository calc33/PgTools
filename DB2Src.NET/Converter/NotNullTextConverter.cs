using System;
using System.Globalization;
using System.Windows.Data;

namespace Db2Source
{
    public class NotNullTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value != null && (bool)value) ? "○" : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
