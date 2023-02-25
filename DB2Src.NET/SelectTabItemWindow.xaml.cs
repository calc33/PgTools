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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// SelectTabItemWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectTabItemWindow: Window
    {
        public static readonly DependencyProperty TabControlProperty = DependencyProperty.Register("TabControl", typeof(TabControl), typeof(SelectTabItemWindow));
        public static readonly DependencyProperty SelectedTabItemProperty = DependencyProperty.Register("SelectedTabItem", typeof(TabItem), typeof(SelectTabItemWindow));
        public SelectTabItemWindow()
        {
            InitializeComponent();
            new CloseOnDeactiveWindowHelper(this, false);
        }

        public TabControl TabControl
        {
            get
            {
                return (TabControl)GetValue(TabControlProperty);
            }
            set
            {
                SetValue(TabControlProperty, value);
            }
        }

        public TabItem SelectedItem
        {
            get
            {
                return (TabItem)GetValue(SelectedTabItemProperty);
            }
            set
            {
                SetValue(SelectedTabItemProperty, value);
            }
        }

        public static FrameworkElement FindElement(FrameworkElement target, string name)
        {
            if (target == null)
            {
                return null;
            }
            if (target.Name == name)
            {
                return target;
            }
            int n = VisualTreeHelper.GetChildrenCount(target);
            for (int i = 0; i < n; i++)
            {
                FrameworkElement elem = VisualTreeHelper.GetChild(target, i) as FrameworkElement;
                FrameworkElement ret = FindElement(elem, name);
                if (ret != null)
                {
                    return ret;
                }
            }
            Decorator dec = target as Decorator;
            if (dec != null)
            {
                FrameworkElement elem = FindElement(dec.Child as FrameworkElement, name);
                if (elem != null)
                {
                    return elem;
                }
            }
            ContentControl cctl = target as ContentControl;
            if (cctl != null)
            {
                FrameworkElement elem = FindElement(cctl.Content as FrameworkElement, name);
                if (elem != null)
                {
                    return elem;
                }
            }
            Control ctl = target as Control;
            if (ctl != null)
            {
                FrameworkElement elem = ctl.Template?.FindName(name, ctl) as FrameworkElement;
                if (elem != null)
                {
                    return elem;
                }
            }
            Panel pnl = target as Panel;
            if (pnl != null)
            {
                foreach (UIElement elem in pnl.Children)
                {
                    FrameworkElement ret = FindElement(elem as FrameworkElement, name);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
            }
            return null;
        }

        private void UpdateWidth()
        {
            //Control ch = FindElement(Grid, "PART_FillerColumnHeader") as Control;
            if (TabControl != null)
            {
                FontFamily = TabControl.FontFamily;
                FontStyle = TabControl.FontStyle;
                FontWeight = TabControl.FontWeight;
                FontStretch = TabControl.FontStretch;
                FontSize = TabControl.FontSize;
            }
            Typeface font = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            Size s = Size.Empty;
            foreach (TabItem c in TabControl.Items)
            {
                if (c.Header == null)
                {
                    continue;
                }
                FormattedText txt = new FormattedText(c.Header.ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font, FontSize, Foreground);
                s = new Size(Math.Max(s.Width, txt.Width), Math.Max(s.Height, txt.Height));
            }
            //listBoxColumns.MinWidth = s.Width + 31;    // 10(ListBoxItemのPadding+BorderTickness) + 17(Scrollbar.Width) + 4(Border類のサイズ)
            listBoxColumns.Width = s.Width + 36; // +5だけ余裕をみておく
        }
        private void UpdateFilter()
        {
            string key = textBoxFilter.Text?.ToUpper();
            if (TabControl == null)
            {
                listBoxColumns.ItemsSource = null;
                return;
            }
            List<TabItem> l = new List<TabItem>();
            TabItem lastSel = listBoxColumns.SelectedItem as TabItem;
            TabItem sel = null;
            foreach (TabItem c in TabControl.Items)
            {
                string s = c.Header?.ToString();
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(key) || s.ToUpper().Contains(key))
                {
                    l.Add(c);
                    if (c == lastSel)
                    {
                        sel = c;
                    }
                }
            }
            listBoxColumns.ItemsSource = l;
            if (sel == null && 0 < l.Count)
            {
                sel = l[0];
            }
            listBoxColumns.SelectedItem = sel;
        }

        private void GridPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateWidth();
            UpdateFilter();
        }
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TabControlProperty)
            {
                GridPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        private void CommitSelection()
        {
            SelectedItem = listBoxColumns.SelectedItem as TabItem;
            if (SelectedItem == null)
            {
                return;
            }
            Dispatcher.InvokeAsync(Close, DispatcherPriority.ApplicationIdle);
        }
        private void CancelSelection()
        {
            SelectedItem = null;
            Dispatcher.InvokeAsync(Close, DispatcherPriority.ApplicationIdle);
        }
        private void listBoxColumns_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CommitSelection();
        }

        private void window_Deactivated(object sender, EventArgs e)
        {
            if (IsVisible)
            {
                SelectedItem = null;
            }
        }

        private void textBoxFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFilter();
        }

        private void ExecuteArrowKey(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    if (0 < listBoxColumns.SelectedIndex)
                    {
                        listBoxColumns.SelectedIndex--;
                        e.Handled = true;
                    }
                    break;
                case Key.Down:
                    List<TabItem> l = listBoxColumns.ItemsSource as List<TabItem>;
                    if (listBoxColumns.SelectedIndex < l.Count - 1)
                    {
                        listBoxColumns.SelectedIndex++;
                        e.Handled = true;
                    }
                    break;
                case Key.Enter:
                    CommitSelection();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    CancelSelection();
                    e.Handled = true;
                    break;
            }
        }

        private void textBoxFilter_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ExecuteArrowKey(e);
        }

        private void window_KeyDown(object sender, KeyEventArgs e)
        {
            ExecuteArrowKey(e);
        }

        private void window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                textBoxFilter.SelectAll();
                textBoxFilter.Focus();
            }
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowLocator.AdjustMaxHeightToScreen(this);
        }

        private void window_LocationChanged(object sender, EventArgs e)
        {
            WindowLocator.AdjustMaxHeightToScreen(this);
        }
    }
}
