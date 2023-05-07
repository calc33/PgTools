using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Db2Source
{
    public class ItemsSourceToColumnFilterButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollection source = value as ICollection;
            if (source != null && 0 < source.Count)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
