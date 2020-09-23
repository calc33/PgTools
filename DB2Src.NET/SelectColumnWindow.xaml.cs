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

        private void UpdateFilter()
        {
            string key = textBoxFilter.Text?.ToUpper();
            if (Grid == null)
            {
                listBoxColumns.ItemsSource = null;
                return;
            }
            List<DataGridColumn> l = new List<DataGridColumn>();
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
                }
            }
            listBoxColumns.ItemsSource = l;
        }

        private void GridPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
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
        private void listBoxColumns_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectedColumn = listBoxColumns.SelectedItem as DataGridColumn;
            _closing = true;
            Close();
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
    }
}
