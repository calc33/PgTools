using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Db2Source
{
    public class LevelWidthConverter : IValueConverter
    {
        private const double WIDTH_PER_LEVEL = 10.0;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return new GridLength(WIDTH_PER_LEVEL);
            }
            try
            {
                return new GridLength((System.Convert.ToDouble(value) + 1) * WIDTH_PER_LEVEL);
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
