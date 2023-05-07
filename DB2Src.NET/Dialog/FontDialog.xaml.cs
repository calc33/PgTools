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
        public static readonly DependencyProperty TypefacesProperty = DependencyProperty.Register("Typefaces", typeof(FamilyTypeface[]), typeof(FontDialog));
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
        
        public FamilyTypeface[] Typefaces
        {
            get { return (FamilyTypeface[])GetValue(TypefacesProperty); }
            set { SetValue(TypefacesProperty, value); }
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

        private static readonly FamilyTypeface[] EmptyTypefaces = new FamilyTypeface[0];
        private void UpdateTypelaces()
        {
            FontFamily selected = listBoxFontFamily.SelectedItem as FontFamily;
            if (selected == null)
            {
                Typefaces = EmptyTypefaces;
                return;
            }
            FamilyTypeface last = null;
            List<FamilyTypeface> l = new List<FamilyTypeface>();
            foreach (FamilyTypeface typeface in selected.FamilyTypefaces)
            {
                if (last != null && !typeface.Equals(last))
                {
                    l.Add(typeface);
                }
                last = typeface;
            }
            Typefaces = l.ToArray();
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
            UpdateTypelaces();
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
}
