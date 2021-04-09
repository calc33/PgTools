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
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class ColumnFilterWindow: Window
    {
        public static readonly DependencyProperty GridProperty = DependencyProperty.Register("Grid", typeof(DataGrid), typeof(ColumnFilterWindow));
        public static readonly DependencyProperty StartIndexProperty = DependencyProperty.Register("StartIndex", typeof(int), typeof(ColumnFilterWindow));
        private List<ColumnItem> _columns = null;
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
        public int StartIndex
        {
            get
            {
                return (int)GetValue(StartIndexProperty);
            }
            set
            {
                SetValue(StartIndexProperty, value);
            }
        }

        private void UpdateColumns()
        {
            _columns = new List<ColumnItem>();
            if (Grid != null)
            {
                for (int i = StartIndex; i < Grid.Columns.Count; i++)
                {
                    DataGridColumn col = Grid.Columns[i];
                    _columns.Add(new ColumnItem(col));
                }
            }
            listBoxColumns.ItemsSource = _columns;
        }
        private void GridPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateColumns();
        }
        private void StartIndexPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateColumns();
        }
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == GridProperty)
            {
                GridPropertyChanged(e);
            }
            if (e.Property == StartIndexProperty)
            {
                StartIndexPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public ColumnFilterWindow()
        {
            InitializeComponent();
        }

        //private object _draggedData;

        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var itemsControl = sender as ItemsControl;
            var draggedItem = e.OriginalSource as FrameworkElement;

            if (itemsControl == null || draggedItem == null)
            { return; }

            //_draggedData = itemsControl.GetItemData(draggedItem);
            //if (_draggedData == null)
            //{ return; }

            //_initialPosition = this.PointToScreen(e.GetPosition(this));
            //_mouseOffsetFromItem = itemsControl.PointToItem(draggedItem, _initialPosition.Value);
            //_draggedItemIndex = itemsControl.GetItemIndex(_draggedData);

        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            foreach (ColumnItem item in _columns)
            {
                item.Apply();
            }
            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
    public class ColumnItem
    {
        private DataGridColumn _column;
        public DataGridColumn Column
        {
            get { return _column; }
            set
            {
                _column = value;
                if (_column != null)
                {
                    IsVisible = (_column.Visibility == Visibility.Visible);
                    Header = _column.Header;
                }
            }
        }
        public bool IsVisible { get; set; }
        public object Header { get; set; }
        public ColumnItem() { }
        public ColumnItem(DataGridColumn column)
        {
            Column = column;
        }
        public void Apply()
        {
            if (Column == null)
            {
                return;
            }
            Column.Visibility = IsVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
