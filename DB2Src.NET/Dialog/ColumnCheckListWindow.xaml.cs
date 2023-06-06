using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// ColumnCheckListWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ColumnCheckListWindow : Window
    {
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(ObservableCollection<ColumnItem>), typeof(ColumnCheckListWindow));

        public ColumnCheckListWindow()
        {
            InitializeComponent();
            Columns = new ObservableCollection<ColumnItem>();
            new CloseOnDeactiveWindowHelper(this, true);
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight * 0.5;
        }

        private JoinTable _target;
        public JoinTable Target
        {
            get
            {
                return _target;
            }
            set
            {
                if (_target == value)
                {
                    return;
                }
                _target = value;
                UpdateColumns();
            }
        }

        public ObservableCollection<ColumnItem> Columns
        {
            get
            {
                return (ObservableCollection<ColumnItem>)GetValue(ColumnsProperty);
            }
            set
            {
                SetValue(ColumnsProperty, value);
            }
        }

        private void UpdateColumns()
        {
            if (Target == null)
            {
                return;
            }
            Dictionary<Column, bool> dict = new Dictionary<Column, bool>();
            foreach (Column c in Target.VisibleColumns)
            {
                dict.Add(c, true);
            }
            if (Columns == null)
            {
                Columns = new ObservableCollection<ColumnItem>();
            }
            else
            {
                Columns.Clear();
            }
            Column[] cols = Target.Table.Columns.GetVisibleColumns(HiddenLevel.Visible);
            foreach (Column c in cols)
            {
                Columns.Add(new ColumnItem() { Column = c, IsChecked = dict.ContainsKey(c) });
            }
        }

        private void CommitVisibleColumns()
        {
            if (Target == null)
            {
                return;
            }
            List<Column> l = new List<Column>();
            foreach (ColumnItem item in Columns)
            {
                if (item.IsChecked)
                {
                    l.Add(item.Column);
                }
            }
            Target.VisibleColumns = l.ToArray();
        }

        protected override void OnClosed(EventArgs e)
        {
            CommitVisibleColumns();
            base.OnClosed(e);
        }

        private void MenuItemCheckAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (ColumnItem item in Columns)
            {
                item.IsChecked = true;
            }
        }

        private void MenuItemUnCheckAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (ColumnItem item in Columns)
            {
                item.IsChecked = false;
            }
        }

        private void listBoxMain_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                ColumnItem sel = listBoxMain.SelectedItem as ColumnItem;
                if (sel == null)
                {
                    return;
                }
                bool flag = !sel.IsChecked;
                foreach (ColumnItem item in listBoxMain.SelectedItems)
                {
                    item.IsChecked = flag;
                }
                e.Handled = true;
            }
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowLocator.AdjustMaxHeightToScreen(this);
        }
    }
}
