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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Db2Source
{
    /// <summary>
    /// SortFieldListControl.xaml の相互作用ロジック
    /// </summary>
    public partial class SortFieldListControl: UserControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Selectable), typeof(SortFieldListControl), new PropertyMetadata(new PropertyChangedCallback(OnTargetPropertyChanged)));

        public Selectable Target
        {
            get
            {
                return (Selectable)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

        private void InitColumns()
        {
            if (Target == null)
            {
                return;
            }
            SetColumns((Target as Table).PrimaryKey?.Columns);
        }

        private void UpdateSortFieldControls()
        {
            foreach (SortFieldControl c in stackPanelMain.Children)
            {
                c.Columns = Target?.Columns;
            }
        }

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateSortFieldControls();
            InitColumns();
        }

        private static void OnTargetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as SortFieldListControl)?.OnTargetPropertyChanged(e);
        }

        internal SortFieldControl AddNewFieldControl(Column column, Order order)
        {
            SortFieldControl c = new SortFieldControl();
            c.Columns = Target?.Columns;

            c.SelectedField = column;
            c.Order = order;
            stackPanelMain.Children.Add(c);
            stackPanelMain.UpdateLayout();
            ScrollToEnd();
            return c;
        }

        private SortFieldControl GetEmptyControl()
        {
            foreach (SortFieldControl c in stackPanelMain.Children)
            {
                if (c.SelectedField == null)
                {
                    return c;
                }
            }
            return null;
        }
        private bool AddSortFieldControl(string columnName, Order order)
        {
            Column col = Target.Columns[columnName];
            if (col == null)
            {
                return false;
            }
            AddNewFieldControl(col, Order.Asc);
            return true;
        }
        public void SetColumns(string[] columns)
        {
            if (Target == null)
            {
                return;
            }
            if (columns == null)
            {
                AddNewFieldControl(null, Order.Asc);
                return;
            }
            stackPanelMain.Children.Clear();
            foreach (string c in columns)
            {
                Column col = Target.Columns[c];
                if (col == null)
                {
                    continue;
                }
                AddNewFieldControl(col, Order.Asc);
                //SortFieldControl ctl = GetEmptyControl();
                //ctl.SelectedField = col;
            }
            AddNewFieldControl(null, Order.Asc);
        }

        public string GetOrderBySql(string prefix)
        {
            string pre = string.IsNullOrEmpty(prefix) ? string.Empty : prefix + ".";
            bool needDelim = false;
            StringBuilder buf = new StringBuilder();
            foreach (SortFieldControl c in stackPanelMain.Children)
            {
                Column col = c.SelectedField;
                if (col != null)
                {
                    if (needDelim)
                    {
                        buf.Append(", ");
                    }
                    buf.Append(pre);
                    buf.Append(col.Name);
                    if (c.Order == Order.Desc)
                    {
                        buf.Append(" desc");
                    }
                    needDelim = true;
                }
            }
            return buf.ToString();
        }

        private double MaxHorizontalOffset()
        {
            return Math.Max(0.0, stackPanelMain.ActualWidth - scrollViewerMain.ActualWidth);
        }
        private double GetPreviousPosition()
        {
            double x = 0.0;
            double x1 = scrollViewerMain.HorizontalOffset;
            foreach (Control c in stackPanelMain.Children)
            {
                if (x1 <= x + c.ActualWidth)
                {
                    return x;
                }
                x += c.ActualWidth;
            }
            return 0.0;
        }
        private double GetNextPosition()
        {
            double x = 0.0;
            double x1 = scrollViewerMain.HorizontalOffset;
            foreach (Control c in stackPanelMain.Children)
            {
                if (x1 < x)
                {
                    x = Math.Min(MaxHorizontalOffset(), x);
                    return x;
                }
                x += c.ActualWidth;
            }
            return MaxHorizontalOffset();
        }

        public void ScrollToNext()
        {
            scrollViewerMain.ScrollToHorizontalOffset(GetNextPosition());
        }
        public void ScrollToPrevious()
        {
            scrollViewerMain.ScrollToHorizontalOffset(GetPreviousPosition());
        }

        public void ScrollToEnd()
        {
            scrollViewerMain.ScrollToHorizontalOffset(MaxHorizontalOffset());
        }

        public SortFieldListControl()
        {
            InitializeComponent();
        }

        private void buttonScrolllLeft_Click(object sender, RoutedEventArgs e)
        {
            ScrollToPrevious();
        }

        private void buttonScrolllRight_Click(object sender, RoutedEventArgs e)
        {
            ScrollToNext();
        }

        private void scrollViewerMain_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            buttonScrolllLeft.IsEnabled = (0.0 < scrollViewerMain.ContentHorizontalOffset);
            buttonScrolllRight.IsEnabled = (scrollViewerMain.ContentHorizontalOffset < stackPanelMain.ActualWidth - scrollViewerMain.ActualWidth);
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            //AddNewFieldControl(null, Order.Asc);
        }
    }
}
