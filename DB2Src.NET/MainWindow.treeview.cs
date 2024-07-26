using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Db2Source
{
    partial class MainWindow
    {
        public static readonly DependencyProperty IsCheckableProperty = DependencyProperty.RegisterAttached("IsCheckable", typeof(bool), typeof(MainWindow), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, IsCheckablePropertyChanged));
        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.RegisterAttached("IsChecked", typeof(bool?), typeof(MainWindow), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, IsCheckedPropertyChanged));
        public static readonly DependencyProperty MultipleSelectionModeProperty = DependencyProperty.RegisterAttached("MultipleSelectionMode", typeof(bool), typeof(MainWindow), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, MultipleSelectionModePropertyChanged));
        public static readonly DependencyProperty ShowOwnerProperty = DependencyProperty.RegisterAttached("ShowOwner", typeof(bool), typeof(MainWindow), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, ShowOwnerPropertyChanged));

        public static bool GetIsCheckable(TreeViewItem target)
        {
            return (bool)target.GetValue(IsCheckableProperty);
        }

        public static void SetIsCheckable(TreeViewItem target, bool value)
        {
            target.SetValue(IsCheckableProperty, value);
        }

        private static void IsCheckablePropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {

        }

        public static bool? GetIsChecked(TreeViewItem target)
        {
            return (bool?)target.GetValue(IsCheckedProperty);
        }

        public static void SetIsChecked(TreeViewItem target, bool? value)
        {
            if (GetIsChecked(target) == value)
            {
                return;
            }
            target.SetValue(IsCheckedProperty, value);
        }

        private static void AdjustIsCheckedByChildren(TreeViewItem target)
        {
            if (target == null)
            {
                return;
            }
            if (!GetIsCheckable(target))
            {
                return;
            }
            try
            {
                bool hasChecked = false;
                bool hasUnchecked = false;
                foreach (TreeViewItem item in target.Items)
                {
                    if (item.Visibility != Visibility.Visible)
                    {
                        continue;
                    }
                    bool? v = GetIsChecked(item);
                    hasChecked |= !v.HasValue || v.Value;
                    hasUnchecked |= !v.HasValue || !v.Value;
                    if (hasChecked && hasUnchecked)
                    {
                        SetIsChecked(target, null);
                        return;
                    }
                }
                SetIsChecked(target, hasChecked);
            }
            finally
            {
                AdjustIsCheckedByChildren(target.Parent as TreeViewItem);
            }
        }
        private static void IsCheckedPropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            TreeViewItem target = dp as TreeViewItem;
            if (target == null)
            {
                return;
            }
            if (e.NewValue == e.OldValue)
            {
                return;
            }
            if (!((bool?)e.NewValue).HasValue)
            {
                return;
            }
            bool v = (bool)e.NewValue;
            foreach (TreeViewItem item in target.Items)
            {
                if (item.Visibility != Visibility.Visible)
                {
                    continue;
                }
                if (!GetIsCheckable(item))
                {
                    continue;
                }
                if (GetIsChecked(item) != v)
                {
                    SetIsChecked(item, v);
                }
            }
            AdjustIsCheckedByChildren(target.Parent as TreeViewItem);
        }

        public static bool? GetMultipleSelectionMode(TreeView target)
        {
            return (bool?)target.GetValue(MultipleSelectionModeProperty);
        }

        public static void SetMultipleSelectionMode(TreeView target, bool? value)
        {
            if (GetMultipleSelectionMode(target) == value)
            {
                return;
            }
            target.SetValue(MultipleSelectionModeProperty, value);
        }

        private static void MultipleSelectionModePropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {

        }

        public static bool? GetShowOwner(TreeView target)
        {
            return (bool?)target.GetValue(ShowOwnerProperty);
        }

        public static void SetShowOwner(TreeView target, bool? value)
        {
            if (GetShowOwner(target) == value)
            {
                return;
            }
            target.SetValue(ShowOwnerProperty, value);
        }

        private static void ShowOwnerPropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {

        }

        //private Dictionary<Tuple<Type, int>, string> groupNodeTypeToStyleName = new Dictionary<Tuple<Type, int>, string>()
        //{
        //    { new Tuple<Type,int>(typeof(Schema),0), "TreeViewItemStyleSchema" },
        //    { new Tuple<Type,int>(typeof(Table),0), "TreeViewItemStyleTables" },
        //    { new Tuple<Type,int>(typeof(View),0), "TreeViewItemStyleTables" },
        //    { new Tuple<Type,int>(typeof(Type_),0), "TreeViewItemStyleTables" },
        //    { new Tuple<Type,int>(typeof(StoredFunction),0), "TreeViewItemStyleTables" },
        //    { new Tuple<Type,int>(typeof(StoredFunction),1), "TreeViewItemStyleTables" },
        //    { new Tuple<Type,int>(typeof(Sequence),0), "TreeViewItemStyleTables" },
        //};
        //private Dictionary<Tuple<Type, int>, string> objectNodeTypeToStyleName = new Dictionary<Tuple<Type, int>, string>()
        //{
        //    { new Tuple<Type,int>(typeof(Schema),0), "TreeViewItemStyleSchema" },
        //    { new Tuple<Type,int>(typeof(Table),0), "TreeViewItemStyleTable" },
        //    { new Tuple<Type,int>(typeof(View),0), "TreeViewItemStyleTable" },
        //    { new Tuple<Type,int>(typeof(Type_),0), "TreeViewItemStyleTable" },
        //    { new Tuple<Type,int>(typeof(BasicType),0), "TreeViewItemStyleTable" },
        //    { new Tuple<Type,int>(typeof(EnumType),0), "TreeViewItemStyleTable" },
        //    { new Tuple<Type,int>(typeof(RangeType),0), "TreeViewItemStyleTable" },
        //    { new Tuple<Type,int>(typeof(ComplexType),0), "TreeViewItemStyleTable" },
        //    { new Tuple<Type,int>(typeof(StoredFunction),0), "TreeViewItemStyleTable" },
        //    { new Tuple<Type,int>(typeof(StoredFunction),1), "TreeViewItemStyleTable" },
        //    { new Tuple<Type,int>(typeof(Sequence),0), "TreeViewItemStyleTable" },
        //};

        //public event DependencyPropertyChangedEventHandler ShowOwnerPropertyChanged;
        //protected void OnShowOwnerPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    ShowOwnerPropertyChanged?.Invoke(sender, e);
        //}
        //private static void ShowOwnerPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    (d as MainWindow)?.OnShowOwnerPropertyChanged(d, e);
        //}

        //public bool ShowOwner
        //{
        //    get { return (bool)GetValue(ShowOwnerProperty); }
        //    set { SetValue(ShowOwnerProperty, value); }
        //}

        private void SetTreeView(TreeViewItem item, TreeNode node)
        {
            item.Tag = node;
            item.FontWeight = node.IsBold ? FontWeights.Bold : FontWeights.Normal;
            item.Header = node.Name;
            item.Style = FindResource("TreeViewItemStyleSchema") as Style;
            if (node.IsGrouped)
            {
                if (node.TargetType == typeof(Schema))
                {
                    if (node.IsHidden)
                    {
                        item.Style = FindResource("TreeViewItemStyleGrayedSchema") as Style;
                        item.HeaderTemplate = FindResource("ImageHiddenSchema") as DataTemplate;
                    }
                    else
                    {
                        item.HeaderTemplate = FindResource("ImageSchema") as DataTemplate;
                    }
                }
                else if (node.TargetType == typeof(Database))
                {
                    Database db = node.Target as Database;
                    if (db == null)
                    {
                        SetIsCheckable(item, false);
                        item.HeaderTemplate = FindResource("ImageDatabase") as DataTemplate;
                        item.Style = FindResource("TreeViewItemStyleGrayedSchema") as Style;
                    }
                    else if (db.IsCurrent)
                    {
                        item.HeaderTemplate = FindResource("ImageDatabase") as DataTemplate;
                        //item.MouseDoubleClick += TreeViewItemDatabase_MouseDoubleClick;
                        item.MouseDoubleClick += TreeViewItem_MouseDoubleClick;
                    }
                    else
                    {
                        SetIsCheckable(item, false);
                        item.HeaderTemplate = FindResource("ImageOtherDatabase") as DataTemplate;
                        item.Style = FindResource("TreeViewItemStyleGrayedSchema") as Style;
                        item.ContextMenu = new ContextMenu();
                        string user = App.GetDefaultLoginUserForDatabase(db);
                        MenuItem mi1 = new MenuItem
                        {
                            Header = user,
                            Tag = db.GetConnectionInfoFor(CurrentDataSet.ConnectionInfo, user),
                            HeaderStringFormat = (string)FindResource("ConnectDatabaseFormat"),
                            FontWeight = FontWeights.Bold
                        };
                        mi1.Click += MenuItemOtherDatabase_Click;
                        item.ContextMenu.Items.Add(mi1);

                        string dba = db.DbaUserName;
                        if (dba != user)
                        {
                            MenuItem mi2 = new MenuItem
                            {
                                Header = dba,
                                Tag = db.GetConnectionInfoFor(CurrentDataSet.ConnectionInfo, dba),
                                HeaderStringFormat = (string)FindResource("ConnectDatabaseFormat")
                            };
                            mi2.Click += MenuItemOtherDatabase_Click;
                            item.ContextMenu.Items.Add(mi2);
                        }
                        MenuItem mi3 = new MenuItem
                        {
                            Header = (string)FindResource("ConnectDatabaseNewUser"),
                            Tag = db.GetConnectionInfoFor(CurrentDataSet.ConnectionInfo, user)
                        };
                        mi3.Click += MenuItemOtherDatabaseNoUser_Click;
                        item.ContextMenu.Items.Add(mi3);
                        item.MouseDoubleClick += TreeViewItemOtherDatabase_MouseDoubleClick;
                    }
                }
                else
                {
                    item.HeaderTemplate = FindResource("ImageTables") as DataTemplate;
                }
            }
            else
            {
                item.HeaderTemplate = FindResource("ImageTable") as DataTemplate;
                item.MouseDoubleClick += TreeViewItem_MouseDoubleClick;
            }
            item.ToolTip = node.Hint;
            //Binding b = new Binding("Tag.Target.CommentText");
            //b.RelativeSource = RelativeSource.Self;
            //item.SetBinding(FrameworkElement.ToolTipProperty, b);
            if (node.Children != null)
            {
                foreach (TreeNode chNode in node.Children)
                {
                    TreeViewItem chItem = new TreeViewItem();
                    SetTreeView(chItem, chNode);
                    item.Items.Add(chItem);
                }
            }
        }

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem src = App.FindVisualParent<TreeViewItem>(e.OriginalSource as DependencyObject);
            TreeViewItem item = sender as TreeViewItem;
            if (src != item)
            {
                // DoubleClickイベントはツリーをたどって親ノードでも呼びされるため、親ノードでの実行時には何もしない
                return;
            }
            if (item == null)
            {
                return;
            }
            TreeNode node = item.Tag as TreeNode;
            if (node == null)
            {
                return;
            }
            SchemaObject obj = node.Target as SchemaObject;
            if (obj == null)
            {
                return;
            }
            e.Handled = true;
            OpenViewer(obj);
        }

        private void MenuItemOtherDatabase_Click(object sender, RoutedEventArgs e)
        {
            NpgsqlConnectionInfo info = (sender as MenuItem).Tag as NpgsqlConnectionInfo;
            if (info == null)
            {
                return;
            }
            App.OpenDatabase(info, false);
        }

        private void MenuItemOtherDatabaseNoUser_Click(object sender, RoutedEventArgs e)
        {
            NpgsqlConnectionInfo info = (sender as MenuItem).Tag as NpgsqlConnectionInfo;
            if (info == null)
            {
                return;
            }
            App.OpenDatabase(info, true);
        }

        private void TreeViewItemOtherDatabase_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            if (item == null)
            {
                return;
            }
            if (item.ContextMenu.Items.Count == 0)
            {
                return;
            }
            MenuItem defaultItem = item.ContextMenu.Items[0] as MenuItem;
            if (defaultItem == null)
            {
                return;
            }
            NpgsqlConnectionInfo info = defaultItem.Tag as NpgsqlConnectionInfo;
            if (info == null)
            {
                return;
            }
            App.OpenDatabase(info, true);
        }


        private void AddTreeView(TreeNode[] nodes)
        {
            treeViewItemTop.Header = CurrentDataSet.GetTreeNodeHeader();
            treeViewItemTop.Items.Clear();
            foreach (TreeNode node in nodes)
            {
                TreeViewItem item = new TreeViewItem();
                SetTreeView(item, node);
                treeViewItemTop.Items.Add(item);
            }
            FilterTreeView(true);
        }

        private string GetCurrentTreeViewFilterText()
        {
            bool filterByName = menuItemFilterByObjectName.IsChecked;
            bool filterByColumnName = menuItemFilterByColumnName.IsChecked;
            return (filterByName || filterByColumnName) ? textBoxFilter.Text : string.Empty;
        }

        private string _treeViewFilterText;
        public void FilterTreeView(bool force)
        {
            _filterTreeViewTimer.Stop();
            bool filterByName = menuItemFilterByObjectName.IsChecked;
            bool filterByColumnName = menuItemFilterByColumnName.IsChecked;
            string txt = GetCurrentTreeViewFilterText();
            if (!force && _treeViewFilterText == txt)
            {
                return;
            }
            _treeViewFilterText = txt;
            string f = _treeViewFilterText.ToUpper();
            foreach (TreeViewItem itemDb in treeViewItemTop.Items)
            {
                foreach (TreeViewItem itemSc in itemDb.Items)
                {
                    foreach (TreeViewItem itemGr in itemSc.Items)
                    {
                        for (int i = itemGr.Items.Count - 1; 0 <= i; i--)
                        {
                            TreeViewItem item = (TreeViewItem)itemGr.Items[i];
                            SchemaObject o = (item.Tag as TreeNode)?.Target as SchemaObject;
                            if (o == null || o.IsReleased)
                            {
                                itemGr.Items.RemoveAt(i);
                            }
                        }
                        int n = 0;
                        foreach (TreeViewItem item in itemGr.Items)
                        {
                            bool matched = false;
                            if (string.IsNullOrEmpty(f))
                            {
                                matched = true;
                            }
                            else
                            {
                                if (filterByName && ((string)item.Header).ToUpper().Contains(f))
                                {
                                    matched = true;
                                }
                                if (filterByColumnName)
                                {
                                    Selectable o = (item.Tag as TreeNode)?.Target as Selectable;
                                    if (o != null)
                                    {
                                        foreach (Column c in o.Columns)
                                        {
                                            if (c.Name.ToUpper().Contains(f))
                                            {
                                                matched = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            if (matched)
                            {
                                item.Visibility = Visibility.Visible;
                                n++;
                            }
                            else
                            {
                                item.Visibility = Visibility.Collapsed;
                            }
                        }
                        TreeNode nodeGr = itemGr.Tag as TreeNode;
                        if (nodeGr.ShowChildCount)
                        {
                            itemGr.Header = string.Format(nodeGr.NameBase, n);
                        }
                        AdjustIsCheckedByChildren(itemGr);
                    }
                }
            }
        }

        private DispatcherTimer _filterTreeViewTimer;
        private DateTime _filterTreeViewExecTime = DateTime.MaxValue;
        private void RequireFilterTreeViewTimer()
        {
            if (_filterTreeViewTimer != null)
            {
                return;
            }
            _filterTreeViewTimer = new DispatcherTimer();
            _filterTreeViewTimer.Tick += FilterTreeViewTimer_Tick;
            _filterTreeViewTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
        }

        private void FilterTreeViewTimer_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now < _filterTreeViewExecTime)
            {
                return;
            }
            _filterTreeViewTimer.Stop();
            _filterTreeViewExecTime = DateTime.MaxValue;
            string txt = GetCurrentTreeViewFilterText();
            if (!string.IsNullOrEmpty(txt))
            {
                FilterTreeView(false);
            }
        }

        private void DelayedFilterTreeView()
        {
            RequireFilterTreeViewTimer();
            _filterTreeViewExecTime = DateTime.Now.AddSeconds(0.3);
            if (!_filterTreeViewTimer.IsEnabled)
            {
                _filterTreeViewTimer.Start();
            }
        }

        private void UpdateTreeViewDB()
        {
            if (CurrentDataSet == null)
            {
                return;
            }
            TreeViewStatusStore store = new TreeViewStatusStore(treeViewDB, treeViewItemTop);
            try
            {
                TreeNode[] nodes = CurrentDataSet.GetVisualTree();
                AddTreeView(nodes);
            }
            finally
            {
                treeViewDB.UpdateLayout();
                if (store.IsEmpty)
                {
                    treeViewItemTop.IsExpanded = true;

                    TreeViewItem itemDb = null;
                    foreach (TreeViewItem item in treeViewItemTop.Items)
                    {
                        TreeNode node = item.Tag as TreeNode;
                        Database db = node?.Target as Database;
                        if (db != null && db.IsCurrent)
                        {
                            itemDb = item;
                            itemDb.IsExpanded = true;
                            break;
                        }
                    }
                    string cur = CurrentDataSet.CurrentSchema;
                    if (itemDb != null && !string.IsNullOrEmpty(cur))
                    {
                        foreach (TreeViewItem item in itemDb.Items)
                        {
                            if (item.Header.ToString() == cur)
                            {
                                item.IsExpanded = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    store.Restore(treeViewDB, treeViewItemTop);
                }
            }
        }
    }
}
