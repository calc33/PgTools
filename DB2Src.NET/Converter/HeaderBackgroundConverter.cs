using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Db2Source
{
    public class HeaderBackgroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush _headerColor = new SolidColorBrush(Color.FromRgb(240, 255, 240));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ColumnValue obj = value as ColumnValue;
            if (obj == null)
            {
                return SystemColors.WindowBrush;
            }
            return obj.IsHeader ? _headerColor : SystemColors.WindowBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
