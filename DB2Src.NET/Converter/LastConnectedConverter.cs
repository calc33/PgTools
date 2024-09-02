using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Db2Source
{
    public class LastConnectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] p = parameter.ToString().Split(';');
            string prefix = string.Empty;
            string fmt = CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern;
            string noneText = "---";
            switch (p.Length)
            {
                case 0:
                    break;
                case 1:
                    prefix = p[0];
                    break;
                case 2:
                    prefix = p[0];
                    fmt = p[1];
                    break;
                default:
                    prefix = p[0];
                    fmt = p[1];
                    noneText = p[2];
                    break;
            }
            if (value == null)
            {
                return prefix + noneText;
            }
            if (!(value is DateTime))
            {
                return value.ToString();
            }
            DateTime v = (DateTime)value;
            if (v.ToOADate() <= 1.0)
            {
                return prefix + noneText;
            }
            return prefix + v.ToString(fmt);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
