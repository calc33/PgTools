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
}
