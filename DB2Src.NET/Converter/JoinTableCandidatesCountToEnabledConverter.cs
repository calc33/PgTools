using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace Db2Source
{
    public class JoinTableCandidatesCountToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollection l = value as ICollection;
            if (l == null)
            {
                return false;
            }
            return 0 < l.Count;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
