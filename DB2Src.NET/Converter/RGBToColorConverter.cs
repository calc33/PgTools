using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Db2Source
{
    public class RGBToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            byte r = (byte)values[0];
            byte g = (byte)values[1];
            byte b = (byte)values[2];
            return Color.FromRgb(r, g, b);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            Color c = (Color)value;
            return new object[] { c.R, c.G, c.B };
        }
    }
}
