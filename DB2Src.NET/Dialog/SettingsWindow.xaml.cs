using System;
using System.Collections;
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
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            WindowLocator.LocateNearby(sender as FrameworkElement, window, NearbyLocation.DownLeft);
            window.SelectFont(target);
            dataGridFont.ItemsSource = null;
            dataGridFont.ItemsSource = _fonts;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowLocator.AdjustMaxHeightToScreen(this);
            dataGridColumnFontButton.CellTemplate = Resources["FontButtonTemplate"] as DataTemplate;
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            WindowLocator.AdjustMaxHeightToScreen(this);
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

    public class OptionItem
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
    public class SettingsItem
    {
        public string Header { get; set; }
        public object Value { get; set; }
        public OptionItem[] Options { get; set; }
        public SettingsItem() { }
    }
    public class SettingsItemCollection : IList<SettingsItem>
    {
        List<SettingsItem> _list = new List<SettingsItem>();

        public SettingsItem this[int index] { get => ((IList<SettingsItem>)_list)[index]; set => ((IList<SettingsItem>)_list)[index] = value; }

        public int Count => ((ICollection<SettingsItem>)_list).Count;

        public bool IsReadOnly => ((ICollection<SettingsItem>)_list).IsReadOnly;

        public void Add(SettingsItem item)
        {
            ((ICollection<SettingsItem>)_list).Add(item);
        }

        public void Clear()
        {
            ((ICollection<SettingsItem>)_list).Clear();
        }

        public bool Contains(SettingsItem item)
        {
            return ((ICollection<SettingsItem>)_list).Contains(item);
        }

        public void CopyTo(SettingsItem[] array, int arrayIndex)
        {
            ((ICollection<SettingsItem>)_list).CopyTo(array, arrayIndex);
        }

        public IEnumerator<SettingsItem> GetEnumerator()
        {
            return ((IEnumerable<SettingsItem>)_list).GetEnumerator();
        }

        public int IndexOf(SettingsItem item)
        {
            return ((IList<SettingsItem>)_list).IndexOf(item);
        }

        public void Insert(int index, SettingsItem item)
        {
            ((IList<SettingsItem>)_list).Insert(index, item);
        }

        public bool Remove(SettingsItem item)
        {
            return ((ICollection<SettingsItem>)_list).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<SettingsItem>)_list).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }
    }
}
