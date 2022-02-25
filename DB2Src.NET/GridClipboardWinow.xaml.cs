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
        public static readonly DependencyProperty ClipboardProperty = DependencyProperty.Register("Clipboard", typeof(GridClipboard), typeof(GridClipboardWindow));
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
            for (int i = 0; i < Clipboard.Fields.Length; i++)
            {
                ColumnInfo fld = Clipboard.Fields[i];
                DataGridTextColumn col = new DataGridTextColumn();
                col.Header = fld;
                col.Binding = new Binding(string.Format("[{0}].Text", i));
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

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ClipboardProperty)
            {
                OnClipboardPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        private void checkBoxContainsHeader_Checked(object sender, RoutedEventArgs e)
        {
            UpdateDataGridClipboardData();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private Size _maxSize;
        private bool _maxSizeInitialized = false;
        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            Rect r = WindowLocator.GetWorkingAreaOf(this);
            _maxSize = r.Size;
            _maxSizeInitialized = true;
            MaxWidth = _maxSize.Width / 2;
            MaxHeight = _maxSize.Height * 2 / 3;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void window_LayoutUpdated(object sender, EventArgs e)
        {
            if (_maxSizeInitialized && SizeToContent != SizeToContent.Manual)
            {
                SizeToContent = SizeToContent.Manual;
                Rect r = WindowLocator.GetWorkingAreaOf(this);
                MaxWidth = r.Width;
                MaxHeight = r.Height;
            }
        }
    }
    public class DataGridCellToCellConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DataGridCell cell = value as DataGridCell;
            if (cell == null)
            {
                return null;
            }
            ColumnInfo col = cell.Column.Header as ColumnInfo;
            if (col == null || col.Index == -1)
            {
                return null;
            }
            GridClipboard.Row row = cell.DataContext as GridClipboard.Row;
            if (row == null)
            {
                return ColumnInfo.Stub;
            }
            return row.Cells[cell.Column.DisplayIndex];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
