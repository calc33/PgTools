using System;
using System.Globalization;
using System.Windows.Data;

namespace Db2Source
{
    public class TimestampTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTime))
            {
                return value;
            }
            DateTime dt0 = DateTime.Now;
            DateTime dt = (DateTime)value;
            TimeSpan t = dt0 - dt;
            if (t.TotalSeconds < 60.0)
            {
                return string.Format("{0}秒前", (int)t.TotalSeconds);
            }
            if (t.TotalMinutes < 60.0)
            {
                return string.Format("{0}分前", (int)t.TotalMinutes);
            }
            if (t.TotalHours < 24.0)
            {
                return string.Format("{0}時間{1}分前", (int)t.TotalHours, t.Minutes);
            }
            if (t.TotalDays < 10.0)
            {
                return string.Format("{0}日前", (int)t.TotalDays);
            }
            if (dt0.Year == dt.Year)
            {
                return string.Format("{0:M/d}", dt);
            }
            return string.Format("{0:yyyy/M/d}", dt);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
