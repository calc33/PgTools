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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Db2Source
{
    /// <summary>
    /// ColorPickerControl.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorPickerControl: UserControl
    {
        //public static readonly DependencyProperty ColorProperty = DependencyProperty.Register("Color", typeof(Color), typeof(ColorPickerControl));
        //public static readonly DependencyProperty ColorBrushProperty = DependencyProperty.Register("ColorBrush", typeof(SolidColorBrush), typeof(ColorPickerControl));
        public static readonly DependencyProperty RGBProperty = DependencyProperty.Register("RGB", typeof(Color), typeof(ColorPickerControl), new PropertyMetadata(new PropertyChangedCallback(OnRGBPropertyChanged)));
        public static readonly DependencyProperty HSVProperty = DependencyProperty.Register("HSV", typeof(Color), typeof(ColorPickerControl), new PropertyMetadata(new PropertyChangedCallback(OnHSVPropertyChanged)));
        public static readonly DependencyProperty RedProperty = DependencyProperty.Register("Red", typeof(byte), typeof(ColorPickerControl));
        public static readonly DependencyProperty GreenProperty = DependencyProperty.Register("Green", typeof(byte), typeof(ColorPickerControl));
        public static readonly DependencyProperty BlueProperty = DependencyProperty.Register("Blue", typeof(byte), typeof(ColorPickerControl));
        public static readonly DependencyProperty HueProperty = DependencyProperty.Register("Hue", typeof(int), typeof(ColorPickerControl));
        public static readonly DependencyProperty SatulationProperty = DependencyProperty.Register("Satulation", typeof(int), typeof(ColorPickerControl));
        public static readonly DependencyProperty BrightnessProperty = DependencyProperty.Register("Brightness", typeof(int), typeof(ColorPickerControl));

        public Color Color
        {
            get
            {
                return RGB;
            }
            set
            {
                RGB = value;
                HSV = value;
            }
        }

        public Color RGB
        {
            get
            {
                return (Color)GetValue(RGBProperty);
            }
            set
            {
                SetValue(RGBProperty, value);
            }
        }

        public Color HSV
        {
            get
            {
                return (Color)GetValue(HSVProperty);
            }
            set
            {
                SetValue(HSVProperty, value);
            }
        }

        public byte Red
        {
            get
            {
                return (byte)GetValue(RedProperty);
            }
            set
            {
                SetValue(RedProperty, value);
            }
        }

        public byte Green
        {
            get
            {
                return (byte)GetValue(GreenProperty);
            }
            set
            {
                SetValue(GreenProperty, value);
            }
        }

        public byte Blue
        {
            get
            {
                return (byte)GetValue(BlueProperty);
            }
            set
            {
                SetValue(BlueProperty, value);
            }
        }

        public int Hue
        {
            get
            {
                return (int)GetValue(HueProperty);
            }
            set
            {
                SetValue(HueProperty, value);
            }
        }

        public int Satulation
        {
            get
            {
                return (int)GetValue(SatulationProperty);
            }
            set
            {
                SetValue(SatulationProperty, value);
            }
        }

        public int Brightness
        {
            get
            {
                return (int)GetValue(BrightnessProperty);
            }
            set
            {
                SetValue(BrightnessProperty, value);
            }
        }

        private void OnRGBPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (tabControlMain.SelectedItem == tabItemRGB)
            {
                HSV = RGB;
            }
        }

        private static void OnRGBPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as ColorPickerControl).OnRGBPropertyChanged(e);
        }

        private void OnHSVPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (tabControlMain.SelectedItem == tabItemHSV)
            {
                RGB = HSV;
            }
        }

        private static void OnHSVPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as ColorPickerControl).OnHSVPropertyChanged(e);
        }

        public ColorPickerControl()
        {
            InitializeComponent();
            MultiBinding mb1 = new MultiBinding()
            {
                Bindings = {
                    new Binding("Red") { RelativeSource = RelativeSource.Self, Mode = BindingMode.TwoWay },
                    new Binding("Green") { RelativeSource = RelativeSource.Self, Mode = BindingMode.TwoWay },
                    new Binding("Blue") { RelativeSource = RelativeSource.Self, Mode = BindingMode.TwoWay }
                },
                Converter = new RGBToColorConverter(),
                Mode = BindingMode.TwoWay,
            };
            SetBinding(RGBProperty, mb1);
            MultiBinding mb2 = new MultiBinding()
            {
                Bindings = {
                    new Binding("Hue") { RelativeSource = RelativeSource.Self, Mode = BindingMode.TwoWay },
                    new Binding("Satulation") { RelativeSource = RelativeSource.Self, Mode = BindingMode.TwoWay },
                    new Binding("Brightness") { RelativeSource = RelativeSource.Self, Mode = BindingMode.TwoWay }
                },
                Converter = new HSBToColorConverter(),
                Mode = BindingMode.TwoWay,
            };
            SetBinding(HSVProperty, mb2);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tb = sender as TextBox;
                tb.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }
        }
    }
    public class RGBToColorConverter: IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            byte r = (byte)values[0];
            byte g = (byte)values[1];
            byte b = (byte)values[2];
            return Color.FromRgb(r, g, b);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            Color c = (Color)value;
            return new object[] { c.R, c.G, c.B };
        }
    }
    public class HSBToColorConverter: IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int h = (int)values[0];
            int s = (int)values[1];
            int b = (int)values[2];
            HSV hsv = new HSV(h, s / 240f, b / 240f);
            RGB rgb = hsv.ToRGB();
            return Color.FromRgb(rgb.R, rgb.G, rgb.B);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            Color c = (Color)value;
            HSV hsv = new HSV(new RGB(c.R, c.G, c.B));
            int h = Math.Max(0, Math.Min((int)(hsv.H + 0.5f), 360));
            int s = Math.Max(0, Math.Min((int)(hsv.S * 240f + 0.5f), 240));
            int b = Math.Max(0, Math.Min((int)(hsv.V * 240f + 0.5f), 240));
            return new object[] { h, s, b };
        }
    }
    public class HueSliderBrushConverter: IValueConverter
    {
        private static Color HToColor(float h, HSV baseColor)
        {
            HSV hsv = new HSV(h, baseColor.S, baseColor.V);
            RGB rgb = hsv.ToRGB();
            return Color.FromRgb(rgb.R, rgb.G, rgb.B);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color c = (Color)value;
            HSV hsv = new HSV(new RGB(c.R, c.G, c.B));
            GradientStopCollection l = new GradientStopCollection()
            {
                new GradientStop(HToColor(0f, hsv), 0.0),
                new GradientStop(HToColor(60f, hsv), 1.0/6.0),
                new GradientStop(HToColor(120f, hsv), 2.0/6.0),
                new GradientStop(HToColor(180f, hsv), 3.0/6.0),
                new GradientStop(HToColor(240f, hsv), 4.0/6.0),
                new GradientStop(HToColor(270f, hsv), 5.0/6.0),
                new GradientStop(HToColor(0f, hsv), 1.0),
            };
            return new LinearGradientBrush(l) { StartPoint = new Point(0.0, 0.5), EndPoint = new Point(1.0, 0.5) };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class SatulationSliderBrushConverter: IValueConverter
    {
        private static Color SToColor(float s, HSV baseColor)
        {
            HSV hsv = new HSV(baseColor.H, s, baseColor.V);
            RGB rgb = hsv.ToRGB();
            return Color.FromRgb(rgb.R, rgb.G, rgb.B);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color c = (Color)value;
            HSV hsv = new HSV(new RGB(c.R, c.G, c.B));
            return new LinearGradientBrush(SToColor(0f, hsv), SToColor(1f, hsv), 0.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BrightnessSliderBrushConverter: IValueConverter
    {
        private static Color VToColor(float v, HSV baseColor)
        {
            HSV hsv = new HSV(baseColor.H, baseColor.S, v);
            RGB rgb = hsv.ToRGB();
            return Color.FromRgb(rgb.R, rgb.G, rgb.B);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color c = (Color)value;
            HSV hsv = new HSV(new RGB(c.R, c.G, c.B));
            return new LinearGradientBrush(VToColor(0f, hsv), VToColor(1f, hsv), 0.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
