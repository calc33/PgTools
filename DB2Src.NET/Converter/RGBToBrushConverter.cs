using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Db2Source
{
    public class RGBToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is RGB))
            {
                return Brushes.Transparent;
            }
            RGB rgb = (RGB)value;
            return new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
