using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Db2Source
{
    public class HueSliderBrushConverter : IValueConverter
    {
        private static Color HToColor(float h, HSV baseColor)
        {
            HSV hsv = new HSV(h, baseColor.S, baseColor.V);
            RGB rgb = hsv.ToRGB();
            return Color.FromRgb(rgb.R, rgb.G, rgb.B);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color c = (Color)value;
            HSV hsv = new HSV(new RGB(c.R, c.G, c.B));
            GradientStopCollection l = new GradientStopCollection()
            {
                new GradientStop(HToColor(0f, hsv), 0.0),
                new GradientStop(HToColor(60f, hsv), 1.0/6.0),
                new GradientStop(HToColor(120f, hsv), 2.0/6.0),
                new GradientStop(HToColor(180f, hsv), 3.0/6.0),
                new GradientStop(HToColor(240f, hsv), 4.0/6.0),
                new GradientStop(HToColor(270f, hsv), 5.0/6.0),
                new GradientStop(HToColor(0f, hsv), 1.0),
            };
            return new LinearGradientBrush(l) { StartPoint = new Point(0.0, 0.5), EndPoint = new Point(1.0, 0.5) };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
