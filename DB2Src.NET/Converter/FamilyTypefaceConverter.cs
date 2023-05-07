using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Db2Source
{
    public class FamilyTypefaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FamilyTypeface f = value as FamilyTypeface;
            if (f == null)
            {
                return value;
            }
            string s1 = FontPack.GetFontWeightStyleText(f.Weight, f.Style);
            string s2 = FontPack.GetFontStretchText(f.Stretch);
            if (!string.IsNullOrEmpty(s1) && !string.IsNullOrEmpty(s2))
            {
                return s1 + FontPack.WeightStyleDelimiter + s2;
            }
            return s1 + s2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
