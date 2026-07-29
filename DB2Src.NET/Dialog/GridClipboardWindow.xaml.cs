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
    /// GridClipboardWinow.xaml の相互作用ロジック
    /// </summary>
    public partial class GridClipboardWindow : Window
    {
        public class ColumnItem: DependencyObject
        {
            public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(ColumnInfo), typeof(ColumnItem));
            public static readonly DependencyProperty NameProperty = DependencyProperty.Register("Name", typeof(string), typeof(ColumnItem));
            public ColumnInfo Value
            {
                get
                {
                    return (ColumnInfo)GetValue(ValueProperty);
                }
                set
                {
                    SetValue(ValueProperty, value);
                }
            }
            public string Name
            {
                get
                {
                    return (string)GetValue(NameProperty);
                }
                set
                {
                    SetValue(NameProperty, value);
                }
            }

            internal ColumnItem(ColumnInfo value)
            {
                Value = value;
                Name = value.Name;
            }

            public ColumnItem() { }

            public override string ToString()
            {
                return Name;
            }
        }

        public static readonly DependencyProperty ClipboardProperty = DependencyProperty.Register("Clipboard", typeof(GridClipboard), typeof(GridClipboardWindow), new PropertyMetadata(new PropertyChangedCallback(OnClipboardPropertyChanged)));
        public static readonly DependencyProperty GridControllerProperty = DependencyProperty.RegisterAttached("GridController", typeof(DataGridController), typeof(GridClipboardWindow), new PropertyMetadata(new PropertyChangedCallback(OnGridControllerPropertyChanged)));
        public static readonly DependencyProperty DisplayColumnsProperty = DependencyProperty.RegisterAttached("DisplayColumns", typeof(ColumnItem[]), typeof(GridClipboardWindow));
        public static readonly DependencyProperty CellInfoProperty = DependencyProperty.RegisterAttached("CellInfo", typeof(GridClipboard.Cell), typeof(GridClipboardWindow));

        public static GridClipboard.Cell GetCellInfo(DependencyObject obj)
        {
            return (GridClipboard.Cell)obj.GetValue(CellInfoProperty);
        }
        public static void SetCellInfo(DependencyObject obj, GridClipboard.Cell value)
        {
            obj.SetValue(CellInfoProperty, value);
        }

        public GridClipboard Clipboard
        {
            get
            {
                return (GridClipboard)GetValue(ClipboardProperty);
            }
            set
            {
                SetValue(ClipboardProperty, value);
            }
        }

        public DataGridController GridController
        {
            get
            {
                return (DataGridController)GetValue(GridControllerProperty);
            }
            set
            {
                SetValue(GridControllerProperty, value);
            }
        }

        public ColumnItem[] DisplayColumns
        {
            get
            {
                return (ColumnItem[])GetValue(DisplayColumnsProperty);
            }
            set
            {
                SetValue(DisplayColumnsProperty, value);
            }
        }

        public GridClipboardWindow()
        {
            InitializeComponent();
        }

        private void UpdateDataGridClipboardDataColumns()
        {
            dataGridClipboardData.Columns.Clear();
            if (Clipboard == null)
            {
                return;
            }
            for (int i = 0; i < Clipboard.Columns.Length; i++)
            {
                GridClipboard.Column fld = Clipboard.Columns[i];
                DataGridTextColumn col = new DataGridTextColumn();
                col.Header = fld;
                col.Binding = new Binding(string.Format("[{0}].Text", i)) { Mode=BindingMode.OneWay };
                col.HeaderStyle = FindResource("DataGridTextColumnHeaderStyle1") as Style;
                dataGridClipboardData.Columns.Add(col);
            }
        }

        private void UpdateDataGridClipboardData()
        {
            UpdateDataGridClipboardDataColumns();
            dataGridClipboardData.ItemsSource = Clipboard.Cells;
        }

        private void OnClipboardPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateDataGridClipboardData();
        }

        private static void OnClipboardPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as GridClipboardWindow)?.OnClipboardPropertyChanged(e);
        }

        private static int CompareDataGridColumnByDisplayIndex(DataGridColumn x, DataGridColumn y)
        {
            return x.DisplayIndex.CompareTo(y.DisplayIndex);
        }
        private static ColumnItem[] GetDisplayColumns(DataGrid grid, string nullText)
        {
            List<DataGridColumn> l = new List<DataGridColumn>(grid.Columns);
            List<ColumnItem> lRet = new List<ColumnItem>();
            l.Sort(CompareDataGridColumnByDisplayIndex);
            lRet.Add(new ColumnItem() { Value = null, Name = nullText });
            if (grid == null)
            {
                return lRet.ToArray();
            }
            foreach (DataGridColumn col in l)
            {
                if (col.DisplayIndex == -1 || col.Visibility != Visibility.Visible)
                {
                    continue;
                }
                ColumnInfo info = col.Header as ColumnInfo;
                if (info != null)
                {
                    lRet.Add(new ColumnItem(info));
                }
            }
            return lRet.ToArray();
        }

        private void UpdateDisplayColumns()
        {
            if (GridController == null)
            {
                DisplayColumns = Array.Empty<ColumnItem>();
                return;
            }
            DisplayColumns = GetDisplayColumns(GridController?.Grid, "(なし)");
        }

        private void OnGridControllerPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateDisplayColumns();
        }

        private static void OnGridControllerPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as GridClipboardWindow)?.OnGridControllerPropertyChanged(e);
        }

        private void checkBoxContainsHeader_Checked(object sender, RoutedEventArgs e)
        {
            UpdateDataGridClipboardData();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowLocator.AdjustMaxSizeToCurrentScreen(this);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void window_LocationChanged(object sender, EventArgs e)
        {
            WindowLocator.AdjustMaxSizeToCurrentScreen(this);
        }

        private void comboBoxHeader_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GridClipboard.Column column = (sender as ComboBox)?.DataContext as GridClipboard.Column;
            if (column == null)
            {
                return;
            }
            foreach (GridClipboard.Column c in Clipboard.Columns)
            {
                if (c == column)
                {
                    continue;
                }
                if (c.TargetColumn == column.TargetColumn)
                {
                    c.TargetColumn = null;
                }
            }
        }
    }
}
