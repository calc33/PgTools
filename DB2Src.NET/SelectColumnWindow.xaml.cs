using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// SelectColumnWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SelectColumnWindow: Window
    {
        public static readonly DependencyProperty GridProperty = DependencyProperty.Register("Grid", typeof(DataGrid), typeof(SelectColumnWindow));
        public static readonly DependencyProperty SelectedColumnProperty = DependencyProperty.Register("SelectedColumn", typeof(DataGridColumn), typeof(SelectColumnWindow));

        public static readonly DependencyProperty IsMovingProperty = DependencyProperty.RegisterAttached("IsMoving", typeof(bool), typeof(SelectColumnWindow));

        public static bool GetIsMoving(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsMovingProperty);
        }
        public static void SetIsMoving(DependencyObject obj, bool value)
        {
            obj.SetValue(IsMovingProperty, value);
        }

        private ListBoxItem _movingItemContainer = null;
        public ListBoxItem MovingItemContainer
        {
            get
            {
                return _movingItemContainer;
            }
            set
            {
                if (_movingItemContainer == value)
                {
                    return;
                }
                if (_movingItemContainer != null)
                {
                    SetIsMoving(_movingItemContainer, false);
                }
                _movingItemContainer = value;
                if (_movingItemContainer != null)
                {
                    SetIsMoving(_movingItemContainer, true);
                }
            }
        }

        public SelectColumnWindow()
        {
            InitializeComponent();
            new CloseOnDeactiveWindowHelper(this, false);
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight * 0.5;
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

        private void CommitSelection()
        {
            SelectedColumn = listBoxColumns.SelectedItem as DataGridColumn;
            if (SelectedColumn == null)
            {
                return;
            }
            Dispatcher.InvokeAsync(Close, DispatcherPriority.ApplicationIdle);
        }
        private void CancelSelection()
        {
            SelectedColumn = null;
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
                SelectedColumn = null;
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

        #region ドラッグ&ドロップによるアイテムの移動

        private struct ListBoxItemIndexRange
        {
            public ScrollViewer Viewer;
            /// <summary>
            /// 一部だけでも入っているItemIndexの最小値
            /// </summary>
            public int PartialMin;
            /// <summary>
            /// 完全に入っているItemIndexの最小値
            /// </summary>
            public int FullMin;
            /// <summary>
            /// 完全に入っているItemIndexの最大値
            /// </summary>
            public int FullMax;
            /// <summary>
            /// 一部だけでも入っているItemIndexの最大値
            /// </summary>
            public int PartialMax;
            public ListBoxItemIndexRange(ListBox listBox)
            {
                ControlTemplate tmpl = listBox.Template as ControlTemplate;
                Viewer = tmpl.FindName("listBoxScrollViewer", listBox) as ScrollViewer;
                PartialMin = int.MaxValue;
                FullMin = int.MaxValue;
                FullMax = -1;
                PartialMax = -1;
                int n = listBox.Items.Count;
                for (int i = 0; i < n; i++)
                {
                    ListBoxItem item = listBox.Items[i] as ListBoxItem;
                    Point[] p = new Point[] { new Point(0, 0), new Point(item.ActualWidth + 1, item.ActualHeight + 1) };
                    p[0] = item.TranslatePoint(p[0], Viewer);
                    p[1] = item.TranslatePoint(p[1], Viewer);
                    if (Viewer.ActualHeight <= p[1].X)
                    {
                        Normalize();
                        return;
                    }
                    if (p[0].Y < Viewer.ActualHeight && 0 < p[1].Y)
                    {
                        PartialMin = Math.Min(PartialMin, i);
                        PartialMax = i;
                    }
                    if (0 <= p[0].Y && p[1].Y < Viewer.ActualHeight)
                    {
                        FullMin = Math.Min(FullMin, i);
                        FullMax = i;
                    }
                }
                Normalize();
            }
            public void Normalize()
            {
                PartialMin = Math.Min(PartialMin, PartialMax);
                FullMin = Math.Min(FullMin, FullMax);
            }
        }

        private void ListBoxItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (MovingItemContainer != null)
            {
                if (e.MouseDevice.LeftButton == MouseButtonState.Released)
                {
                    MovingItemContainer = null;
                }
                return;
            }
            ListBoxItem item = App.FindVisualParent<ListBoxItem>(e.Source as DependencyObject);
            if (item == null)
            {
                return;
            }
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
            {
                ScrollViewer sv = App.FindVisualParent<ScrollViewer>(item);
                if (sv != null && !sv.IsMouseCaptured)
                {
                    Mouse.Capture(sv);
                }
                MovingItemContainer = item;
            }
        }

        private void MoveItem(ListBoxItem goal)
        {
            if (MovingItemContainer == null)
            {
                return;
            }
            if (MovingItemContainer == goal)
            {
                return;
            }
            ObservableCollection<DataGridColumn> l = Grid.Columns;
            int goalPos = l.IndexOf(goal.Content as DataGridColumn);
            int i = l.IndexOf(MovingItemContainer.Content as DataGridColumn);
            //bool sel = MovingItemContainer.IsSelected;
            l.Move(i, goalPos);
            //if (sel)
            //{
            //    listBoxColumns.SelectedItem = MovingItemContainer.Content;
            //}
            listBoxColumns.UpdateLayout();
        }

        private void listBoxColumns_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
            MovingItemContainer = null;
            ListBox viewer = sender as ListBox;
            HitTestResult ret = VisualTreeHelper.HitTest(viewer, e.MouseDevice.GetPosition(viewer));
            if (ret == null)
            {
                return;
            }
            ListBoxItem item = App.FindVisualParent<ListBoxItem>(ret.VisualHit);
            MovingItemContainer = item;
        }

        private void listBoxScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (MovingItemContainer == null)
            {
                return;
            }
            ScrollViewer viewer = sender as ScrollViewer;
            if (viewer == null)
            {
                return;
            }
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed && !viewer.IsMouseCaptured)
            {
                viewer.CaptureMouse();
            }
            FrameworkElement parent = VisualTreeHelper.GetParent(viewer) as FrameworkElement;
            Point p = e.MouseDevice.GetPosition(parent);
            double dy = 0;
            if (p.Y < 0)
            {
                dy = p.Y;
            }
            else if (viewer.ActualHeight < p.Y)
            {
                dy = p.Y - viewer.ActualHeight;
            }
            ListBoxItem goal;
            if (dy == 0)
            {
                HitTestResult ret = VisualTreeHelper.HitTest(viewer, e.MouseDevice.GetPosition(viewer));
                if (ret == null)
                {
                    return;
                }
                goal = App.FindVisualParent<ListBoxItem>(ret.VisualHit);
                double y = e.MouseDevice.GetPosition(goal).Y;
                if (y < 0 || MovingItemContainer.ActualHeight < y)
                {
                    // 移動した結果マウスの位置が選択している項目の範囲外になって振動を起こさないように
                    goal = null;
                }
            }
            else if (dy < 0)
            {
                ListBoxItemIndexRange range = new ListBoxItemIndexRange(listBoxColumns);
                goal = listBoxColumns.Items[Math.Max(0, range.PartialMin - 1)] as ListBoxItem;
            }
            else
            {
                ListBoxItemIndexRange range = new ListBoxItemIndexRange(listBoxColumns);
                goal = listBoxColumns.Items[Math.Min(range.PartialMax + 1, listBoxColumns.Items.Count - 1)] as ListBoxItem;
            }
            if (goal == null || VisualTreeHelper.GetParent(goal) != VisualTreeHelper.GetParent(MovingItemContainer))
            {
                return;
            }
            MoveItem(goal);
        }

        private void listBoxScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer viewer = sender as ScrollViewer;
            if (viewer != null && viewer.IsMouseCaptured)
            {
                viewer.ReleaseMouseCapture();
            }
            MovingItemContainer = null;
        }

        private void listBoxScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MovingItemContainer = null;
            ScrollViewer viewer = sender as ScrollViewer;
            HitTestResult ret = VisualTreeHelper.HitTest(viewer, e.MouseDevice.GetPosition(viewer));
            if (ret == null)
            {
                return;
            }
            Control c = App.FindVisualParent<Control>(ret.VisualHit);
            MovingItemContainer = c as ListBoxItem;
        }
        #endregion
    }
    public class VisibilityToBooleanConverter : IValueConverter
    {
        public VisibilityToBooleanConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((value != null) && (bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
