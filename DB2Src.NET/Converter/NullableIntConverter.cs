using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Db2Source
{
    public class NullableIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            try
            {
                if (value is string)
                {
                    string v = (string)value;
                    if (string.IsNullOrEmpty(v))
                    {
                        return null;
                    }
                    return int.Parse(v);
                }
                return System.Convert.ToInt32(value);
            }
            catch (FormatException)
            {
                return new ValidationResult(false, value);
            }
            catch (InvalidCastException)
            {
                return new ValidationResult(false, value);
            }
            catch (OverflowException)
            {
                return new ValidationResult(false, value);
            }
        }
    }
}
