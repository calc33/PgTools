using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// SettingsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingsWindow: Window
    {
        private FontPack[] _fonts = FontPack.CloneFonts();

        public SettingsWindow()
        {
            InitializeComponent();
            dataGridFont.ItemsSource = _fonts;
        }

        private void ButtonChangeFont_Click(object sender, RoutedEventArgs e)
        {
            FontDialog window = new FontDialog();
            window.Owner = this;
            FontPack target = (sender as FrameworkElement).DataContext as FontPack;
            window.SelectFont(target);
            dataGridFont.ItemsSource = null;
            dataGridFont.ItemsSource = _fonts;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dataGridColumnFontButton.CellTemplate = Resources["FontButtonTemplate"] as DataTemplate;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            for (int i = _fonts.Length - 1; 0 <= i; i--)
            {
                FontPack font = _fonts[i];
                font.CopyTo(font.Source);
            }
            App.Current.SaveSettingToRegistry();
            Dispatcher.InvokeAsync(Close, DispatcherPriority.ApplicationIdle);
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(Close, DispatcherPriority.ApplicationIdle);
        }
    }
}
