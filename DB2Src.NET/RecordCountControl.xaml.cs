using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
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
            //public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(Selectable), typeof(TableRecordCount));
            public static readonly DependencyProperty CountProperty = DependencyProperty.Register("Count", typeof(long?), typeof(TableRecordCount));

            public bool? IsChecked
            {
                get { return (bool)GetValue(IsCheckedProperty); }
                set { SetValue(IsCheckedProperty, value); }
            }

            public Selectable Target { get; set; }

            private long? _count;
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
            private void UpdateCount()
            {
                Count = _count;
            }
            public bool UpdateRecordCount(IDbConnection connection, bool force)
            {
                if (!force && _count.HasValue)
                {
                    return false;
                }
                _count = Target.GetRecordCount(connection);
                Dispatcher.InvokeAsync(UpdateCount);
                return true;
            }
        }

        public class TreeNode : DependencyObject, IComparable
        {
            public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register("IsChecked", typeof(bool?), typeof(TreeNode), new PropertyMetadata(new PropertyChangedCallback(OnIsCheckedPropertyChanged)));
            public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register("IsExpanded", typeof(bool), typeof(TreeNode), new PropertyMetadata(new PropertyChangedCallback(OnIsExpandedPropertyChanged)));
            public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(NamedObject), typeof(TreeNode));
            public static readonly DependencyProperty RecordCountProperty = DependencyProperty.Register("RecordCount", typeof(TableRecordCount), typeof(TreeNode));

            public bool? IsChecked
            {
                get { return (bool?)GetValue(IsCheckedProperty); }
                set { SetValue(IsCheckedProperty, value); }
            }

            public bool IsExpanded
            {
                get { return (bool)GetValue(IsExpandedProperty); }
                set { SetValue(IsExpandedProperty, value); }
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
                Owner.DelayedUpdateDataGridTables();
            }

            public TreeNode(RecordCountControl owner, TreeNode parent, NamedObject target)
            {
                Parent = parent;
                Target = target;
                IsChecked = false;
                IsExpanded = false;
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

            private void OnIsExpandedPropertyChanged(DependencyPropertyChangedEventArgs e)
            {
                Owner?.UpdateListBoxTables();
            }

            private static void OnIsExpandedPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
            {
                (target as TreeNode)?.OnIsExpandedPropertyChanged(e);
            }

            private void OnIsCheckedPropertyChanged(DependencyPropertyChangedEventArgs e)
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

            private static void OnIsCheckedPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
            {
                (target as TreeNode)?.OnIsCheckedPropertyChanged(e);
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

        public static readonly DependencyProperty DataSetProperty = DependencyProperty.Register("DataSet", typeof(Db2SourceContext), typeof(RecordCountControl), new PropertyMetadata(new PropertyChangedCallback(OnDataSetPropertyChanged)));

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

        public string[] SettingCheckBoxNames { get { return StrUtil.EmptyStringArray; } }

        private void OnDataSetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateItems();
        }

        private static void OnDataSetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as RecordCountControl)?.OnDataSetPropertyChanged(e);
        }

        public RecordCountControl()
        {
            InitializeComponent();
            _updateListBoxTablesTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = new TimeSpan(500 * 10000), IsEnabled = false };
            _updateListBoxTablesTimer.Tick += UpdateListBoxTablesTimer_Tick;
            _updateDataGridTablesTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = new TimeSpan(500 * 10000), IsEnabled = false };
            _updateDataGridTablesTimer.Tick += UpdateDataGridTablesTimer_Tick;
            CommandBinding cb = new CommandBinding(DataGridCommands.CopyTable, dataGridTablesCopyTable_Executed, dataGridTablesCopyTable_CanExecute);
            dataGridTables.CommandBindings.Add(cb);
            cb = new CommandBinding(DataGridCommands.CopyTableContent, dataGridTablesCopyTableContent_Executed, dataGridTablesCopyTable_CanExecute);
            dataGridTables.CommandBindings.Add(cb);
        }

        private void dataGridTablesCopyTable_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = dataGridTables.ItemsSource != null;
        }

        private void dataGridTablesCopyTable_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            List<string[]> l = new List<string[]>();
            l.Add(new string[] { dataGridTables.Columns[0].Header.ToString(), dataGridTables.Columns[1].Header.ToString(), dataGridTables.Columns[2].Header.ToString() });
            foreach (TableRecordCount rec in dataGridTables.ItemsSource)
            {
                l.Add(new string[] { rec.Target.SchemaName, rec.Target.Name, rec.Count?.ToString() });
            }
            DataGridController.CopyTableToClipboard(l.ToArray());
        }

        private void dataGridTablesCopyTableContent_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            List<string[]> l = new List<string[]>();
            //l.Add(new string[] { dataGridTables.Columns[0].Header.ToString(), dataGridTables.Columns[1].Header.ToString() });
            foreach (TableRecordCount rec in dataGridTables.ItemsSource)
            {
                l.Add(new string[] { rec.Target.SchemaName, rec.Target.Name, rec.Count?.ToString() });
            }
            DataGridController.CopyTableToClipboard(l.ToArray());
        }

        private void UpdateDataGridTablesTimer_Tick(object sender, EventArgs e)
        {
            UpdateDataGridTables(false);
        }

        public string GetTabItemHeader()
        {
            return (string)FindResource("tabItemHeader");
        }

        private void UpdateListBoxTablesTimer_Tick(object sender, EventArgs e)
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
                    TreeNode nodeSc = new TreeNode(this, null, sc) { IsExpanded = false };
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

        public void AddRange(IEnumerable<Selectable> tables)
        {
            Dictionary<NamedObject, bool> schemaDict = new Dictionary<NamedObject, bool>();
            Dictionary<NamedObject, bool> tableDict = new Dictionary<NamedObject, bool>();
            foreach (Selectable tbl in tables)
            {
                tableDict[tbl] = true;
                schemaDict[tbl.Schema] = true;
            }
            foreach (TreeNode nodeSc in _allEntries)
            {
                if (!schemaDict.ContainsKey(nodeSc.Target))
                {
                    continue;
                }
                foreach (TreeNode nodeTbl in nodeSc.Items)
                {
                    if (!tableDict.ContainsKey(nodeTbl.Target))
                    {
                        continue;
                    }
                    nodeTbl.IsChecked = true;
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
            if (!node.IsExpandable || !node.IsExpanded)
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
        private DispatcherTimer _updateDataGridTablesTimer;

        private void DelayedUpdateListBoxTables()
        {
            _updateListBoxTablesTimer.IsEnabled = true;
        }

        private void DelayedUpdateDataGridTables()
        {
            _updateDataGridTablesTimer.IsEnabled = true;
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

        private void UpdateDataGridTables(bool refresh)
        {
            _updateDataGridTablesTimer.Stop();
            AbortUpdateRecordCountTask(true);
            List<TableRecordCount> l = new List<TableRecordCount>();
            foreach (TreeNode node in _allEntries)
            {
                AddCheckedRecordCount(l, node);
            }
            _checkedItems = l;
            dataGridTables.ItemsSource = null;
            dataGridTables.ItemsSource = _checkedItems;
            Db2SourceContext dataSet = DataSet;
            UpdateRecordCount(refresh);
        }

        private void textBoxFilterTable_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxFilterTable.Text))
            {
                DelayedUpdateListBoxTables();
            }
        }

        private void textBoxFilterTable_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DelayedUpdateListBoxTables();
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
                DelayedUpdateListBoxTables();
            }
        }

        private bool _isButtonExecuteRunning;
        private Task _recordCountTask;
        private bool _isRecordCountTaskCancelling;
        private bool IsButtonExecuteRunning
        {
            get { return _isButtonExecuteRunning; }
            set
            {
                if (_isButtonExecuteRunning == value)
                {
                    return;
                }
                _isButtonExecuteRunning = value;
                Dispatcher.InvokeAsync(() => { buttonExecute.IsChecked = _isButtonExecuteRunning; });
            }
        }

        private void UpdateRecordCount(bool force)
        {
            Db2SourceContext dataSet = DataSet;
            Task t = _recordCountTask;
            if (t != null && !t.IsCompleted)
            {
                return;
            }
            _recordCountTask = Task.Run(() =>
            {
                try
                {
                    _isRecordCountTaskCancelling = false;
                    IsButtonExecuteRunning = true;
                    using (IDbConnection conn = dataSet.NewConnection(true))
                    {
                        List<TableRecordCount> l = _checkedItems;
                        foreach (TableRecordCount rec in l)
                        {
                            if (_isRecordCountTaskCancelling)
                            {
                                break;
                            }
                            if (rec.UpdateRecordCount(conn, force))
                            {
                                Thread.Sleep(10);
                            }
                        }
                    }
                }
                finally
                {
                    IsButtonExecuteRunning = false;
                    _recordCountTask = null;
                    _isRecordCountTaskCancelling = false;
                }
            });
        }
        private void AbortUpdateRecordCountTask(bool waitForAborted)
        {
            _isRecordCountTaskCancelling = true;
            Task t = _recordCountTask;
            if (waitForAborted && t != null && !t.IsCompleted)
            {
                t.Wait();
            }
        }
        private void buttonExecute_Click(object sender, RoutedEventArgs e)
        {
            if (IsButtonExecuteRunning)
            {
                AbortUpdateRecordCountTask(false);
            }
            else
            {
                UpdateDataGridTables(true);
            }
        }

        private void dataGridTables_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //DataGridCell cell = App.FindVisualParent<DataGridCell>(e.OriginalSource as DependencyObject);
            //if (cell != null && cell.Column == dataGridTablesChecked)
            //{
            //    TableRecordCount rec = cell.DataContext as TableRecordCount;
            //    if (rec != null)
            //    {
            //        rec.IsChecked = !rec.IsChecked;
            //    }
            //}
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

        private void buttonCopyTable_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = buttonCopyTable.ContextMenu;
            menu.PlacementTarget = sender as UIElement;
            menu.IsOpen = true;
            foreach (MenuItem mi in menu.Items)
            {
                mi.CommandTarget = dataGridTables;
            }
        }
    }
}
