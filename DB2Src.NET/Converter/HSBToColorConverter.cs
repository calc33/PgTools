using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Db2Source
{
    public class HSBToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int h = (int)values[0];
            int s = (int)values[1];
            int b = (int)values[2];
            HSV hsv = new HSV(h, s / 240f, b / 240f);
            RGB rgb = hsv.ToRGB();
            return Color.FromRgb(rgb.R, rgb.G, rgb.B);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            Color c = (Color)value;
            HSV hsv = new HSV(new RGB(c.R, c.G, c.B));
            int h = Math.Max(0, Math.Min((int)(hsv.H + 0.5f), 360));
            int s = Math.Max(0, Math.Min((int)(hsv.S * 240f + 0.5f), 240));
            int b = Math.Max(0, Math.Min((int)(hsv.V * 240f + 0.5f), 240));
            return new object[] { h, s, b };
        }
    }
}
