﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Db2Source
{
    public class NotNullOrEmptyToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }
            if (value is string)
            {
                return !string.IsNullOrEmpty(((string)value).Trim());
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
