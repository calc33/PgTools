﻿using System;
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

        private void SetTreeView(TreeViewItem item, TreeNode node)
        {
            item.Tag = node;
            item.FontWeight = node.IsBold ? FontWeights.Bold : FontWeights.Normal;
            item.Header = node.Name;
            if (node.IsGrouped)
            {
                if (node.TargetType == typeof(Schema))
                {
                    if (node.IsHidden)
                    {
                        item.Style = Resources["TreeViewItemStyleGrayed"] as Style;
                        item.HeaderTemplate = Resources["ImageHiddenSchema"] as DataTemplate;
                    }
                    else
                    {
                        item.HeaderTemplate = Resources["ImageSchema"] as DataTemplate;
                    }
                }
                else if (node.TargetType == typeof(Database))
                {
                    Database db = node.Target as Database;
                    if (db.IsCurrent)
                    {
                        item.HeaderTemplate = Resources["ImageDatabase"] as DataTemplate;
                        //item.MouseDoubleClick += TreeViewItemDatabase_MouseDoubleClick;
                        item.MouseDoubleClick += TreeViewItem_MouseDoubleClick;
                    }
                    else
                    {
                        item.Tag = db;
                        item.HeaderTemplate = Resources["ImageOtherDatabase"] as DataTemplate;
                        item.Style = Resources["TreeViewItemStyleGrayed"] as Style;
                        item.ContextMenu = new ContextMenu();
                        MenuItem mi = new MenuItem
                        {
                            Header = item.Header,
                            HeaderStringFormat = "{0}に接続"
                        };
                        mi.Click += MenuItemOtherDatabase_Click;
                        item.ContextMenu.Items.Add(mi);
                        item.MouseDoubleClick += TreeViewItemOtherDatabase_MouseDoubleClick;
                    }
                }
                else
                {
                    item.HeaderTemplate = Resources["ImageTables"] as DataTemplate;
                }
            }
            else
            {
                item.HeaderTemplate = Resources["ImageTable"] as DataTemplate;
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

        private void OpenDatabase(Database database)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }
            if (CurrentDataSet == null)
            {
                return;
            }
            NpgsqlConnectionInfo obj = CurrentDataSet.ConnectionInfo as NpgsqlConnectionInfo;
            if (obj == null)
            {
                return;
            }
            string path = Assembly.GetExecutingAssembly().Location;
            string args = string.Format("-h {0} -p {1} -d {2} -U {3}", obj.ServerName, obj.ServerPort, database.Name, obj.UserName);
            Process.Start(path, args);
        }

        private void MenuItemOtherDatabase_Click(object sender, RoutedEventArgs e)
        {
            Database db = (sender as MenuItem).Header as Database;
            if (db == null)
            {
                return;
            }
            OpenDatabase(db);
        }
        private void TreeViewItemOtherDatabase_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Database db = (sender as TreeViewItem).Tag as Database;
            if (db == null)
            {
                return;
            }
            OpenDatabase(db);
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

        private string _treeViewFilterText;
        public void FilterTreeView(bool force)
        {
            if (!force && _treeViewFilterText == textBoxFilter.Text)
            {
                return;
            }
            _treeViewFilterText = textBoxFilter.Text;
            bool filterByName = menuItemFilterByObjectName.IsChecked;
            bool filterByColumnName = menuItemFilterByColumnName.IsChecked;
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
                        if ((nodeGr.ShowChildCount))
                        {
                            itemGr.Header = string.Format(nodeGr.NameBase, n);
                        }
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
            FilterTreeView(true);

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
