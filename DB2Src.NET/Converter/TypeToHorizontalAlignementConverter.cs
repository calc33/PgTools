using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Db2Source
{
    public class TypeToHorizontalAlignementConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return HorizontalAlignment.Left;
            }
            //Type t = value.GetType();
            object v = value;
            if (v is ColumnValue)
            {
                v = ((ColumnValue)v).Value;
            }
            if (v is sbyte || v is byte || v is short || v is ushort
                || v is int || v is uint || v is long || v is ulong
                || v is float || v is double || v is decimal)
            {
                return HorizontalAlignment.Right;
            }
            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
