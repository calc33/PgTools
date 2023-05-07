using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Db2Source
{
    public class SatulationSliderBrushConverter : IValueConverter
    {
        private static Color SToColor(float s, HSV baseColor)
        {
            HSV hsv = new HSV(baseColor.H, s, baseColor.V);
            RGB rgb = hsv.ToRGB();
            return Color.FromRgb(rgb.R, rgb.G, rgb.B);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color c = (Color)value;
            HSV hsv = new HSV(new RGB(c.R, c.G, c.B));
            return new LinearGradientBrush(SToColor(0f, hsv), SToColor(1f, hsv), 0.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
