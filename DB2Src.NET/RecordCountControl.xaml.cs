using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// RecordCountControl.xaml の相互作用ロジック
    /// </summary>
    public partial class RecordCountControl : UserControl, ISchemaObjectWpfControl
    {
        //private static TabItem TabItem;
        private static bool IsTrue(bool? value)
        {
            return value.HasValue && value.Value;
        }
        private static bool IsFalse(bool? value)
        {
            return value.HasValue && !value.Value;
        }

        public class TableRecordCount : DependencyObject, IComparable
        {
            public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool?), typeof(TableRecordCount));
            public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Selectable), typeof(TableRecordCount));
            public static readonly DependencyProperty CountProperty = DependencyProperty.Register("Count", typeof(long?), typeof(TableRecordCount));

            public bool? IsChecked
            {
                get { return (bool)GetValue(IsCheckedProperty); }
                set { SetValue(IsCheckedProperty, value); }
            }

            public Selectable Target
            {
                get { return (Selectable)GetValue(TargetProperty); }
                set { SetValue(TargetProperty, value); }
            }
            public long? Count
            {
                get { return (long?)GetValue(CountProperty); }
                set { SetValue(CountProperty, value); }
            }

            public int CompareTo(object obj)
            {
                if (!(obj is TableRecordCount))
                {
                    return -1;
                }
                return string.Compare(Target?.FullIdentifier, ((TableRecordCount)obj).Target?.FullIdentifier);
            }
        }

        public class TreeNode : DependencyObject, IComparable
        {
            public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool?), typeof(TreeNode));
            public static readonly DependencyProperty IsFoldedProperty = DependencyProperty.Register("IsFolded", typeof(bool), typeof(TreeNode));
            public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(NamedObject), typeof(TreeNode));
            public static readonly DependencyProperty RecordCountProperty = DependencyProperty.Register("RecordCount", typeof(TableRecordCount), typeof(TreeNode));

            public bool? IsChecked
            {
                get { return (bool?)GetValue(IsCheckedProperty); }
                set { SetValue(IsCheckedProperty, value); }
            }

            public bool IsFolded
            {
                get { return (bool)GetValue(IsFoldedProperty); }
                set { SetValue(IsFoldedProperty, value); }
            }
            public bool IsExpandable { get { return FilteredItems.Count != 0; } }
            public bool IsLeaf { get { return Target is Selectable; } }

            private TreeNode _parent;
            public TreeNode Parent
            {
                get { return _parent; }
                internal set
                {
                    _parent = value;
                    if (_parent != null)
                    {
                        Level = _parent.Level + 1;
                    }
                    else
                    {
                        Level = 0;
                    }
                }
            }

            public RecordCountControl Owner { get; internal set; }

            private void UpdateIsCheckedByChildren()
            {
                if (IsLeaf)
                {
                    return;
                }
                bool? v;
                if (!IsExpandable)
                {
                    v = false;
                }
                else
                {
                    bool hasChecked = false;
                    bool hasUnchecked = false;
                    foreach (TreeNode node in FilteredItems)
                    {
                        hasChecked |= node.IsChecked ?? true;
                        hasUnchecked |= !(node.IsChecked ?? false);
                    }
                    if (hasChecked && hasUnchecked)
                    {
                        v = null;
                    }
                    else
                    {
                        v = hasChecked;
                    }
                }
                if (IsChecked == v)
                {
                    return;
                }
                IsChecked = v;
                Parent?.UpdateIsCheckedByChildren();
            }
            private void UpdateIsCheckedByParent()
            {
                if (!Parent.IsChecked.HasValue)
                {
                    return;
                }
                SetValue(IsCheckedProperty, Parent.IsChecked);
                if (IsExpandable)
                {
                    foreach (TreeNode node in FilteredItems)
                    {
                        node.UpdateIsCheckedByParent();
                    }
                }
            }
            private void UpdateIsChecked()
            {
                if (!IsChecked.HasValue)
                {
                    return;
                }
                if (IsExpandable)
                {
                    foreach (TreeNode node in FilteredItems)
                    {
                        node.UpdateIsCheckedByParent();
                    }
                }
                Parent?.UpdateIsCheckedByChildren();
            }

            public TreeNode(RecordCountControl owner, TreeNode parent, NamedObject target)
            {
                Parent = parent;
                Target = target;
                IsChecked = false;
                IsFolded = true;
                Owner = owner;
            }

            public NamedObject Target
            {
                get { return (NamedObject)GetValue(TargetProperty); }
                set { SetValue(TargetProperty, value); }
            }
            public TableRecordCount RecordCount
            {
                get { return (TableRecordCount)GetValue(RecordCountProperty); }
                set { SetValue(RecordCountProperty, value); }
            }

            private static readonly TreeNode[] NO_ITEMS = new TreeNode[0];
            public TreeNode[] Items { get; internal set; } = NO_ITEMS;
            public List<TreeNode> FilteredItems { get; } = new List<TreeNode>();

            public int Level { get; internal set; }

            private void IsFoldedPropertyChanged(DependencyPropertyChangedEventArgs e)
            {
                Owner?.UpdateListBoxTables();
            }

            private void IsCheckedPropertyChanged(DependencyPropertyChangedEventArgs e)
            {
                if (Owner == null)
                {
                    return;
                }
                if (Owner.IsCheckedUpdating)
                {
                    return;
                }
                Owner.IsCheckedUpdating = true;
                try
                {
                    UpdateIsChecked();
                }
                finally
                {
                    Owner.IsCheckedUpdating = false;
                }
            }

            protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
            {
                if (e.Property == IsFoldedProperty)
                {
                    IsFoldedPropertyChanged(e);
                }
                if (e.Property == IsCheckedProperty)
                {
                    IsCheckedPropertyChanged(e);
                }
                base.OnPropertyChanged(e);
            }

            public int CompareTo(object obj)
            {
                if (!(obj is TreeNode))
                {
                    return -1;
                }
                return string.Compare(Target?.FullIdentifier, ((TreeNode)obj).Target?.FullIdentifier);
            }

            public override string ToString()
            {
                return Target?.FullIdentifier;
            }
        }

        internal bool IsCheckedUpdating;

        public static readonly DependencyProperty DataSetProperty = DependencyProperty.Register("DataSet", typeof(Db2SourceContext), typeof(RecordCountControl));

        public Db2SourceContext DataSet
        {
            get { return (Db2SourceContext)GetValue(DataSetProperty); }
            set { SetValue(DataSetProperty, value); }
        }
        private TreeNode[] _allEntries = new TreeNode[0];
        private List<TreeNode> _filteredEntries = new List<TreeNode>();
        //private List<TableRecordCount> _visibleItems { get; set; } = new List<TableRecordCount>();
        private List<TableRecordCount> _checkedItems { get; set; } = new List<TableRecordCount>();
        public SchemaObject Target { get { return null; } set { } }
        public string SelectedTabKey { get { return null; } set { } }

        private void OnDataSetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateItems();
        }


        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == DataSetProperty)
            {
                OnDataSetPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public RecordCountControl()
        {
            InitializeComponent();
            _updateListBoxTablesTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = new TimeSpan(500 * 10000), IsEnabled = false };
            _updateListBoxTablesTimer.Tick += UpdateDataGridTablesTimer_Tick;
        }

        public string GetTabItemHeader()
        {
            return (string)Resources["tabItemHeader"];
        }

        private void UpdateDataGridTablesTimer_Tick(object sender, EventArgs e)
        {
            UpdateListBoxTables();
        }

        private void UpdateItems()
        {
            List<TreeNode> l = new List<TreeNode>();
            if (DataSet != null)
            {
                foreach (Schema sc in DataSet.Schemas)
                {
                    if (sc.IsHidden)
                    {
                        continue;
                    }
                    TreeNode nodeSc = new TreeNode(this, null, sc) { IsFolded = true };
                    List<TreeNode> lTbl = new List<TreeNode>();
                    foreach (SchemaObject obj in sc.Objects)
                    {
                        if (!(obj is Table))
                        {
                            continue;
                        }
                        TableRecordCount rec = new TableRecordCount() { Target = (Selectable)obj };
                        TreeNode node = new TreeNode(this, nodeSc, obj) { RecordCount = rec, IsChecked = false };
                        BindingOperations.SetBinding(rec, TableRecordCount.IsCheckedProperty, new Binding("IsChecked") { Source = node, Mode = BindingMode.TwoWay });
                        lTbl.Add(node);
                    }
                    lTbl.Sort();

                    if (lTbl.Count != 0)
                    {
                        nodeSc.Items = lTbl.ToArray();
                        l.Add(nodeSc);
                    }
                }
            }
            l.Sort();
            _allEntries = l.ToArray();
            UpdateListBoxTables();
        }

        private bool Matches(Selectable table)
        {
            string ft = textBoxFilterTable.Text;
            return (table != null) && (string.IsNullOrEmpty(ft) || table.Name.Contains(ft));
        }

        private void AddMatched(List<TreeNode> list, TreeNode node)
        {
            node.FilteredItems.Clear();
            Selectable tbl = node.Target as Selectable;
            if (tbl == null)
            {
                foreach (TreeNode node2 in node.Items)
                {
                    AddMatched(node.FilteredItems, node2);
                }
                if (node.FilteredItems.Count != 0)
                {
                    list.Add(node);
                }
            }
            else
            {
                if (Matches(tbl))
                {
                    list.Add(node);
                }
            }
        }
        private void UpdateFilteredEntries()
        {
            _filteredEntries.Clear();
            foreach (TreeNode node in _allEntries)
            {
                AddMatched(_filteredEntries, node);
            }
        }

        private void AddToList(List<TreeNode> list, TreeNode node)
        {
            list.Add(node);
            if (!node.IsExpandable || node.IsFolded)
            {
                return;
            }
            foreach (TreeNode subNode in node.FilteredItems)
            {
                AddToList(list, subNode);
            }
        }

        private void UpdateListBoxTables()
        {
            _updateListBoxTablesTimer.Stop();
            UpdateFilteredEntries();
            List<TreeNode> l = new List<TreeNode>();
            foreach (TreeNode node in _filteredEntries)
            {
                AddToList(l, node);
            }
            listBoxTables.ItemsSource = null;
            listBoxTables.ItemsSource = l;
        }

        private DispatcherTimer _updateListBoxTablesTimer;

        private void DelayedUpdateListBoxTables()
        {
            _updateListBoxTablesTimer.IsEnabled = true;
        }

        private void AddCheckedRecordCount(List<TableRecordCount> list, TreeNode node)
        {
            if (IsTrue(node.IsChecked) && node.RecordCount != null)
            {
                list.Add(node.RecordCount);
            }
            foreach (TreeNode subNode in node.Items)
            {
                AddCheckedRecordCount(list, subNode);
            }
        }
        private void UpdateDataGridTables()
        {
            List<TableRecordCount> l = new List<TableRecordCount>();
            foreach (TreeNode node in _allEntries)
            {
                AddCheckedRecordCount(l, node);
            }
            _checkedItems = l;
            dataGridTables.ItemsSource = null;
            dataGridTables.ItemsSource = _checkedItems;
        }

        private void textBoxFilterTable_TextChanged(object sender, TextChangedEventArgs e)
        {
            _updateListBoxTablesTimer.IsEnabled = !string.IsNullOrEmpty(textBoxFilterTable.Text);
        }

        private void textBoxFilterTable_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _updateListBoxTablesTimer.IsEnabled = true;
            }
        }

        private void buttonSearch_Click(object sender, RoutedEventArgs e)
        {
            UpdateFilteredEntries();
        }

        private void textBoxFilterSchema_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _updateListBoxTablesTimer.IsEnabled = true;
            }
        }

        private void buttonExecute_Click(object sender, RoutedEventArgs e)
        {
            UpdateDataGridTables();
            using (IDbConnection conn = DataSet.NewConnection(true))
            {
                foreach (TableRecordCount rec in _checkedItems)
                {
                    rec.Count = rec.Target.GetRecordCount(conn);
                }
            }
        }

        private void dataGridTables_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DataGridCell cell = App.FindVisualParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell != null && cell.Column == dataGridTablesChecked)
            {
                TableRecordCount rec = cell.DataContext as TableRecordCount;
                if (rec != null)
                {
                    rec.IsChecked = !rec.IsChecked;
                }
            }
        }

        private void ToggleSelectedItems()
        {
            TableRecordCount rec0 = dataGridTables.CurrentItem as TableRecordCount;
            if (rec0 == null)
            {
                return;
            }
            bool flag = !(rec0.IsChecked ?? true);
            foreach (object item in dataGridTables.SelectedItems)
            {
                TableRecordCount rec = item as TableRecordCount;
                if (rec == null)
                {
                    continue;
                }
                rec.IsChecked = flag;
            }
        }

        private void dataGridTables_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                ToggleSelectedItems();
                e.Handled = true;
            }
        }

        public void OnTabClosing(object sender, ref bool cancel)
        {

        }

        public void OnTabClosed(object sender)
        {
            //throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
    public class LevelWidthConverter : IValueConverter
    {
        private const double WIDTH_PER_LEVEL = 10.0;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return new GridLength(WIDTH_PER_LEVEL);
            }
            try
            {
                return new GridLength((System.Convert.ToDouble(value) + 1) * WIDTH_PER_LEVEL);
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
