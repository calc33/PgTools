using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Db2Source
{
    public class PgsqlColumnArrayToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            string[] strs = value as string[];
            if (strs.Length == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append('(');
            bool needComma = false;
            foreach (string s in strs)
            {
                if (needComma)
                {
                    buf.Append(", ");
                }
                buf.Append(NpgsqlDataSet.GetEscapedPgsqlIdentifier(s, true));
                needComma = true;
            }
            buf.Append(')');
            return buf.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            string csv = value.ToString();
            if (string.IsNullOrEmpty(csv))
            {
                return csv;
            }
            if (csv[0] == '(')
            {
                csv = csv.Substring(1);
            }
            if (csv[csv.Length - 1] == ')')
            {
                csv = csv.Substring(0, csv.Length - 1);
            }
            string[] strs = csv.Split(',');
            for (int i = 0; i < strs.Length; i++)
            {
                strs[i] = Db2SourceContext.DequoteIdentifier(strs[i].Trim());
            }
            return strs;
        }
    }
}
