using System;
using System.Globalization;
using System.Windows.Data;

namespace Db2Source
{
    public class ColumnInfoConverter : IValueConverter
    {
        public ColumnInfo ColumnInfo { get; set; }
        public ColumnInfoConverter() { }
        internal ColumnInfoConverter(ColumnInfo columnInfo)
        {
            ColumnInfo = columnInfo;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (ColumnInfo == null)
            {
                return value;
            }
            return ColumnInfo.Convert(value, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (ColumnInfo == null)
            {
                return value;
            }
            return ColumnInfo.ConvertBack(value, targetType, parameter, culture);
        }
    }
}
