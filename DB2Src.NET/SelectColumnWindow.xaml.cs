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

namespace Db2Source
{
    /// <summary>
    /// SelectColumnWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectColumnWindow: Window
    {
        public static readonly DependencyProperty GridProperty = DependencyProperty.Register("Grid", typeof(DataGrid), typeof(SelectColumnWindow));
        public static readonly DependencyProperty SelectedColumnProperty = DependencyProperty.Register("SelectedColumn", typeof(DataGridColumn), typeof(SelectColumnWindow));
        public SelectColumnWindow()
        {
            InitializeComponent();
        }

        public DataGrid Grid
        {
            get
            {
                return (DataGrid)GetValue(GridProperty);
            }
            set
            {
                SetValue(GridProperty, value);
            }
        }

        public DataGridColumn SelectedColumn
        {
            get
            {
                return (DataGridColumn)GetValue(SelectedColumnProperty);
            }
            set
            {
                SetValue(SelectedColumnProperty, value);
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
            if (Grid != null)
            {
                FontFamily = Grid.FontFamily;
                FontStyle = Grid.FontStyle;
                FontWeight = Grid.FontWeight;
                FontStretch = Grid.FontStretch;
                FontSize = Grid.FontSize;
            }
            Typeface font = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            Size s = Size.Empty;
            foreach (DataGridColumn c in Grid.Columns)
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
            if (Grid == null)
            {
                listBoxColumns.ItemsSource = null;
                return;
            }
            List<DataGridColumn> l = new List<DataGridColumn>();
            DataGridColumn lastSel = listBoxColumns.SelectedItem as DataGridColumn;
            DataGridColumn sel = null;
            foreach (DataGridColumn c in Grid.Columns)
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
            if (e.Property == GridProperty)
            {
                GridPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        private bool _closing = false;

        private void CommitSelection()
        {
            SelectedColumn = listBoxColumns.SelectedItem as DataGridColumn;
            if (SelectedColumn == null)
            {
                return;
            }
            _closing = true;
            Close();
        }
        private void listBoxColumns_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            CommitSelection();
        }

        private void window_Deactivated(object sender, EventArgs e)
        {
            if (_closing)
            {
                return;
            }
            SelectedColumn = null;
            Close();
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
                    List<DataGridColumn> l = listBoxColumns.ItemsSource as List<DataGridColumn>;
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
    }
}
