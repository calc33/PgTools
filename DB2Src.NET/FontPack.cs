using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace Db2Source
{
    public class FontPack: DependencyObject
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(FontPack));
        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(FontPack));
        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register("FontSize", typeof(double), typeof(FontPack));
        public static readonly DependencyProperty FontStretchProperty = DependencyProperty.Register("FontStretch", typeof(FontStretch), typeof(FontPack));
        public static readonly DependencyProperty FontStyleProperty = DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(FontPack));
        public static readonly DependencyProperty FontWeightProperty = DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(FontPack));
        public static readonly DependencyProperty ParentProperty = DependencyProperty.Register("Parent", typeof(FontPack), typeof(FontPack));

        private void BindParent(DependencyProperty property)
        {
            Binding b = BindingOperations.GetBinding(this, property);
            if (b == null)
            {
                b = new Binding("Parent." + property.Name) { RelativeSource = RelativeSource.Self };
                BindingOperations.SetBinding(this, property, b);
            }
        }

        public FontPack()
        {
            BindParent(FontFamilyProperty);
            BindParent(FontSizeProperty);
            BindParent(FontStretchProperty);
            BindParent(FontStyleProperty);
            BindParent(FontWeightProperty);
        }

        public FontPack(FontPack source):this()
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            Source = source;
            Title = source.Title;
            FontFamily = source.FontFamily;
            FontWeight = source.FontWeight;
            FontStyle = source.FontStyle;
            FontStretch = source.FontStretch;
            FontSize = source.FontSize;
        }

        public string Title
        {
            get
            {
                return (string)GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }

        public FontFamily FontFamily
        {
            get
            {
                return (FontFamily)GetValue(FontFamilyProperty);
            }
            set
            {
                SetValue(FontFamilyProperty, value);
            }
        }

        public double FontSize
        {
            get
            {
                return (double)GetValue(FontSizeProperty);
            }
            set
            {
                SetValue(FontSizeProperty, value);
            }
        }

        public FontStretch FontStretch
        {
            get
            {
                return (FontStretch)GetValue(FontStretchProperty);
            }
            set
            {
                SetValue(FontStretchProperty, value);
            }
        }

        public FontStyle FontStyle
        {
            get
            {
                return (FontStyle)GetValue(FontStyleProperty);
            }
            set
            {
                SetValue(FontStyleProperty, value);
            }
        }

        public FontWeight FontWeight
        {
            get
            {
                return (FontWeight)GetValue(FontWeightProperty);
            }
            set
            {
                SetValue(FontWeightProperty, value);
            }
        }

        public string FontFamilyName
        {
            get
            {
                return FontFamily.Source;
            }
            set
            {
                if (FontFamily != null && FontFamily.Source == value)
                {
                    return;
                }
                FontFamily = new FontFamily(value);
            }
        }
        public double FontSizeValue
        {
            get
            {
                return FontSize;
            }
            set
            {
                if (FontSize == value)
                {
                    return;
                }
                FontSize = value;
            }
        }
        public string FontStretchName
        {
            get
            {
                return FontStretch.ToString();
            }
            set
            {
                FontStretch v = (FontStretch)new FontStretchConverter().ConvertFromString(value);
                if (FontStretch.Equals(v))
                {
                    return;
                }
                FontStretch = v;
            }
        }

        public int FontStretchValue
        {
            get
            {
                return FontStretch.ToOpenTypeStretch();
            }
            set
            {
                FontStretch v = FontStretch.FromOpenTypeStretch(value);
                if (FontStretch.Equals(v))
                {
                    return;
                }
                FontStretch = v;
            }
        }
        public string FontStyleName
        {
            get
            {
                return FontStyle.ToString();
            }
            set
            {
                FontStyle v = (FontStyle)new FontStyleConverter().ConvertFromString(value);
                if (FontStyle.Equals(v))
                {
                    return;
                }
                FontStyle = v;
            }
        }
        public string FontWeightName
        {
            get
            {
                return FontWeight.ToString();
            }
            set
            {
                FontWeight v = (FontWeight)new FontWeightConverter().ConvertFromString(value);
                if (FontWeight.Equals(v))
                {
                    return;
                }
                FontWeight = v;
            }
        }

        public FontPack Parent
        {
            get
            {
                return (FontPack)GetValue(ParentProperty);
            }
            set
            {
                SetValue(ParentProperty, value);
            }
        }

        public FontPack Source { get; set; }

        public void CopyTo(FontPack destination)
        {
            destination.FontFamilyName = FontFamilyName;
            destination.FontWeightName = FontWeightName;
            destination.FontStyleName = FontStyleName;
            destination.FontStretchName = FontStretchName;
            destination.FontSizeValue = FontSizeValue;
        }

        public override string ToString()
        {
            string s;
            string s1 = GetFontWeightStyleText(FontWeight, FontStyle);
            string s2 = GetFontStretchText(FontStretch);
            if (!string.IsNullOrEmpty(s1) && !string.IsNullOrEmpty(s2))
            {
                s = s1 + WeightStyleDelimiter + s2;
            }
            else
            {
                s = s1 + s2;
            }
            return string.Format("{0} {1}, {2:0.0}", GetLocalName(FontFamily, CultureInfo.CurrentUICulture), s, FontSize);
        }

        public static FontPack[] GetFonts()
        {
            return new FontPack[]
            {
                BaseFont,
                CodeFont,
                TreeFont,
                DataGridFont
            };
        }

        /// <summary>
        /// GetFonts()で取得できるフォント設定の編集用クローンを作成する
        /// </summary>
        /// <returns></returns>
        public static FontPack[] CloneFonts()
        {
            FontPack[] original = GetFonts();
            FontPack baseFont = new FontPack();
            FontPack[] fonts = new FontPack[]
            {
                baseFont,
                new FontPack() { Parent = baseFont },
                new FontPack() { Parent = baseFont },
                new FontPack() { Parent = baseFont }
            };
            if (original.Length != fonts.Length)
            {
                throw new NotImplementedException("GetFonts()と矛盾しています");
            }
            for (int i = 0; i < original.Length; i++)
            {
                original[i].CopyTo(fonts[i]);
                fonts[i].Title = original[i].Title;
                fonts[i].Source = original[i];
            }
            return fonts;
        }

        public static FontPack BaseFont
        {
            get
            {
                return (FontPack)App.Current.Resources["BaseFont"];
            }
        }

        public static FontPack CodeFont
        {
            get
            {
                return (FontPack)App.Current.Resources["CodeFont"];
            }
        }

        public static FontPack TreeFont
        {
            get
            {
                return (FontPack)App.Current.Resources["TreeFont"];
            }
        }

        public static FontPack DataGridFont
        {
            get
            {
                return (FontPack)App.Current.Resources["DataGridFont"];
            }
        }

        public static string WeightStyleDelimiter
        {
            get
            {
                return (string)App.Current.Resources["WeightStyleDelimiter"];
            }
        }

        public static TextDictionary FontWeightDictionary
        {
            get
            {
                return (TextDictionary)App.Current.Resources["FontWeightText"];
            }
        }

        public static TextDictionary FontStyleDictionary
        {
            get
            {
                return (TextDictionary)App.Current.Resources["FontStyleText"];
            }
        }

        public static TextDictionary FontStretchDictionary
        {
            get
            {
                return (TextDictionary)App.Current.Resources["FontStretchText"];
            }
        }

        public static TextDictionary OpenTypeStretchDictionary
        {
            get
            {
                return (TextDictionary)App.Current.Resources["OpenTypeStretchText"];
            }
        }

        public static TextDictionary FontWeightStyleDictionary
        {
            get
            {
                return (TextDictionary)App.Current.Resources["FontWeightStyleText"];
            }
        }

        public static string GetFontWeightText(FontWeight weight)
        {
            string v;
            if (!FontWeightDictionary.TryGetValue(weight.ToString(), out v))
            {
                return weight.ToString();
            }
            return v;
        }

        public static string GetFontStyleText(FontStyle style)
        {
            string v;
            if (!FontStyleDictionary.TryGetValue(style.ToString(), out v))
            {
                return style.ToString();
            }
            return v;
        }

        public static string GetFontWeightStyleText(FontWeight weight, FontStyle style)
        {
            string k = weight.ToString() + "-" + style.ToString();
            string v;
            if (!FontWeightStyleDictionary.TryGetValue(k, out v))
            {
                string w = GetFontWeightText(weight);
                string s = GetFontStyleText(style);
                if (!string.IsNullOrEmpty(w) && !string.IsNullOrEmpty(s))
                {
                    return w + WeightStyleDelimiter + s;
                }
                return w + s;
            }
            return v;
        }

        public static string GetFontStretchText(FontStretch stretch)
        {
            string v;
            if (!FontStretchDictionary.TryGetValue(stretch.ToString(), out v))
            {
                if (!OpenTypeStretchDictionary.TryGetValue(stretch.ToOpenTypeStretch().ToString(), out v))
                {
                    return stretch.ToString();
                }
            }
            return v;
        }

        public static string GetLocalName(FontFamily fontFamily, CultureInfo cultureInfo)
        {
            try
            {
                XmlLanguage lang = XmlLanguage.GetLanguage(cultureInfo.Name);
                string v;
                if (fontFamily.FamilyNames.TryGetValue(lang, out v))
                {
                    return v;
                }
            }
            catch (ArgumentException) { }
            return fontFamily.Source;
        }
    }
}
