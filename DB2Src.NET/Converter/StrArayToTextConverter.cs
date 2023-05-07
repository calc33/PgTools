using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Db2Source
{
    public class StrArayToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] strs = value as string[];
            if (strs == null || strs.Length == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(strs[0]);
            for (int i = 1; i < strs.Length; i++)
            {
                string s = strs[i];
                buf.AppendLine();
                buf.Append(s);
            }
            return buf.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
