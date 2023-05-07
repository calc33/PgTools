using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Db2Source
{
    public class IsErrorBrushConverter : IValueConverter
    {
        private static Brush RedBrush = new SolidColorBrush(Colors.Red);
        //private static Brush NormalBrush = SystemColors.WindowTextBrush;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (((value is bool) && (bool)value)) ? RedBrush : SystemColors.WindowTextBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
