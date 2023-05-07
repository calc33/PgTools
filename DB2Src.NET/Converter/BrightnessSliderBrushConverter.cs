using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Db2Source
{
    public class BrightnessSliderBrushConverter : IValueConverter
    {
        private static Color VToColor(float v, HSV baseColor)
        {
            HSV hsv = new HSV(baseColor.H, baseColor.S, v);
            RGB rgb = hsv.ToRGB();
            return Color.FromRgb(rgb.R, rgb.G, rgb.B);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color c = (Color)value;
            HSV hsv = new HSV(new RGB(c.R, c.G, c.B));
            return new LinearGradientBrush(VToColor(0f, hsv), VToColor(1f, hsv), 0.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
