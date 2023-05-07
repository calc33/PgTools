using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Db2Source
{
    public class MultiBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null)
            {
                return Visibility.Visible;
            }
            foreach (object v in values)
            {
                if (v is bool)
                {
                    if (!(bool)v)
                    {
                        return Visibility.Collapsed;
                    }
                }
                if (v is bool?)
                {
                    bool? b = (bool?)v;
                    if (!b.HasValue || !b.Value)
                    {
                        return Visibility.Collapsed;
                    }
                }
            }
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
