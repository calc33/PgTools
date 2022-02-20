using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// FontDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class FontDialog : Window
    {
        public static readonly DependencyProperty FontFamiliesProperty = DependencyProperty.Register("FontFamilies", typeof(FontFamily[]), typeof(FontDialog));
        public static readonly DependencyProperty FontProperty = DependencyProperty.Register("Font", typeof(FontPack), typeof(FontDialog));
        
        public FontDialog()
        {
            InitializeComponent();
            FontFamilies = Fonts.SystemFontFamilies.ToArray();
            Font = new FontPack();
        }

        public FontFamily[] FontFamilies
        {
            get
            {
                return (FontFamily[])GetValue(FontFamiliesProperty);
            }
            set
            {
                SetValue(FontFamiliesProperty, value);
            }
        }
        
        public FontPack Font
        {
            get
            {
                return (FontPack)GetValue(FontProperty);
            }
            set
            {
                SetValue(FontProperty, value);
            }
        }

        public bool? SelectFont(FontPack target)
        {
            Font = new FontPack() { Parent = target };
            bool? ret = ShowDialog();
            if (ret.HasValue && ret.Value)
            {
                Font.CopyTo(target);
            }
            return ret;
        }

        private FamilyTypeface FindTypeface(FamilyTypefaceCollection families, FontWeight weight, FontStyle style, FontStretch stretch)
        {
            foreach (FamilyTypeface family in families)
            {
                if (family.Weight.Equals(weight) && family.Style.Equals(style) && family.Stretch.Equals(stretch))
                {
                    return family;
                }
            }
            return null;
        }

        private FamilyTypeface GetDefaultTypeface()
        {
            FontFamily sel = listBoxFontFamily.SelectedItem as FontFamily;
            if (sel == null || sel.FamilyTypefaces == null || sel.FamilyTypefaces.Count == 0)
            {
                return null;
            }
            FamilyTypeface typeface = FindTypeface(sel.FamilyTypefaces, Font.FontWeight, Font.FontStyle, Font.FontStretch);
            if (typeface != null)
            {
                return typeface;
            }
            return sel.FamilyTypefaces[0];
        }

        private void listBoxFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            listBoxTypefaces.SelectedItem = GetDefaultTypeface();
        }

        private void listBoxTypefaces_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FamilyTypeface typeface = listBoxTypefaces.SelectedItem as FamilyTypeface;
            if (typeface == null)
            {
                return;
            }
            Font.FontWeight = typeface.Weight;
            Font.FontStyle = typeface.Style;
            Font.FontStretch = typeface.Stretch;
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            if (listBoxFontFamily.SelectedItem != null)
            {
                listBoxFontFamily.ScrollIntoView(listBoxFontFamily.SelectedItem);
            }
            if (listBoxTypefaces.SelectedItem != null)
            {
                listBoxTypefaces.ScrollIntoView(listBoxTypefaces.SelectedItem);
            }
            if (listBoxSize.SelectedItem != null)
            {
                listBoxSize.ScrollIntoView(listBoxSize.SelectedItem);
            }
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void UpdateListBoxFontFamily()
        {
            string s = textBoxFilter.Text;
            if (string.IsNullOrEmpty(s))
            {
                listBoxFontFamily.ItemsSource = FontFamilies;
                return;
            }
            s = s.Trim().ToLower();
            List<FontFamily> l = new List<FontFamily>(FontFamilies.Length);
            foreach (FontFamily f in FontFamilies)
            {
                if (f.Source.ToLower().Contains(s))
                {
                    l.Add(f);
                    continue;
                }
                foreach (string fn in f.FamilyNames.Values)
                {
                    if (fn.ToLower().Contains(s))
                    {
                        l.Add(f);
                        break;
                    }
                }
            }
            listBoxFontFamily.ItemsSource = l.ToArray();
        }

        private void textBoxFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateListBoxFontFamily();
        }
    }

    public class FontFamilyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FontFamily f = value as FontFamily;
            if (f == null)
            {
                return value;
            }
            return FontPack.GetLocalName(f, CultureInfo.CurrentUICulture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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
