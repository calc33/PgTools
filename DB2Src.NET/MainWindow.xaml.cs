using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Db2Source
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow: Window
    {
        private string TitleBase;
        public static readonly Type[] ConnectionInfoTypes = new Type[] { typeof(NpgsqlConnectionInfo) };
        public static readonly DependencyProperty CurrentDataSetProperty = DependencyProperty.Register("CurrentDataSet", typeof(Db2SourceContext), typeof(MainWindow));
        public static MainWindow Current { get; private set; } = null;
        public Db2SourceContext CurrentDataSet
        {
            get { return (Db2SourceContext)GetValue(CurrentDataSetProperty); }
            set { SetValue(CurrentDataSetProperty, value); }
        }

        private int _queryControlIndex = 1;
        public MainWindow()
        {
            InitializeComponent();
            if (Current == null)
            {
                Current = this;
            }
        }

        private void MenuItemOpenDb_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi == null)
            {
                return;
            }
            ConnectionInfo info = mi.Tag as ConnectionInfo;
            if (CurrentDataSet != null)
            {
                string path = Assembly.GetExecutingAssembly().Location;
                NpgsqlConnectionInfo obj = info as NpgsqlConnectionInfo;
                if (obj != null)
                {
                    string args = string.Format("-h {0} -p {1} -d {2} -U {3}", obj.ServerName, obj.ServerPort, obj.DatabaseName, obj.UserName);
                    Process.Start(path, args);
                }
                else
                {
                    Process.Start(path);
                }
                return;
            }
            if (info == null)
            {
                info = NewConnectionInfoFromRegistry();
            }
            NewConnectionWindow win = new NewConnectionWindow();
            win.Owner = this;
            win.Target = info;
            bool? ret = win.ShowDialog();
            if (!ret.HasValue || !ret.Value)
            {
                return;
            }
            info = win.Target;
            Connect(info);
            App.Connections.Merge(info);
            App.Connections.Save();
        }
        private void UpdateTabControlsTarget()
        {
            for (int i = tabControlMain.Items.Count - 1; 0 <= i; i--)
            {
                TabItem item = tabControlMain.Items[i] as TabItem;
                UIElement c = item.Content as UIElement;
                Type t = c.GetType();
                PropertyInfo p = t.GetProperty("Target");
                if (p == null)
                {
                    continue;
                }
                SchemaObject o = p?.GetValue(c) as SchemaObject;
                if (o != null)
                {
                    Schema sch = CurrentDataSet.Schemas[o.SchemaName];
                    o = sch?.GetCollection(o.GetCollectionIndex())[o.Identifier] as SchemaObject;
                }
                if (o != null)
                {
                    p.SetValue(c, o);
                }
                else
                {
                    tabControlMain.Items.RemoveAt(i);
                }
            }
        }

        private void UpdateMenuItemOpenDb()
        {
            MenuItemOpenDb.Items.Clear();
            MenuItem mi;
            foreach (ConnectionInfo info in App.Connections)
            {
                mi = new MenuItem() { Header = info.Name, Tag = info };
                mi.Click += MenuItemOpenDb_Click;
                MenuItemOpenDb.Items.Add(mi);
            }
            mi = new MenuItem() { Header = "新しい接続..." };
            mi.Click += MenuItemOpenDb_Click;
            MenuItemOpenDb.Items.Add(mi);
        }

        public static void TabBecomeVisible(FrameworkElement element)
        {
            if (element == null)
            {
                return;
            }
            if (element is Window)
            {
                return;
            }
            TabBecomeVisible(element.Parent as FrameworkElement);
            if (!(element is TabItem))
            {
                return;
            }
            TabItem tab = element as TabItem;
            TabControl ctrl = tab.Parent as TabControl;
            if (ctrl != null && ctrl.SelectedItem != tab)
            {
                ctrl.SelectedItem = tab;
            }
        }

        public static TabItem NewTabItem(TabControl parent, string header, UIElement element, Style tabItemStyle)
        {
            TabItem item = new TabItem();
            item.Content = element;
            item.Header = Db2SourceContext.EscapedHeaderText(header);
            item.Style = tabItemStyle;
            parent.Items.Add(item);
            return item;
        }

        private object TabItemLock = new object();
        public TabItem RequireTabItem(SchemaObject target, Style tabItemStyle)
        {
            Control ctrl = target.Control as Control;
            if (ctrl != null)
            {
                return ctrl.Parent as TabItem;
            }

            lock (TabItemLock)
            {
                if (ctrl != null)
                {
                    return ctrl.Parent as TabItem;
                }
                ctrl = NewControl(target);
                if (ctrl == null)
                {
                    return null;
                }
                TabItem item = NewTabItem(tabControlMain, target.FullName, ctrl, tabItemStyle);
                item.Tag = this;
                return item;
            }
        }
        private static Dictionary<Type, Type> _schemaObjectToControl = new Dictionary<Type, Type>();
        public static void RegisterSchemaObjectControl(Type schemaObjectClass, Type controlClass)
        {
            if (!schemaObjectClass.IsSubclassOf(typeof(SchemaObject)) && schemaObjectClass != typeof(SchemaObject))
            {
                throw new ArgumentException("schemaObjectClassはSchemaObjectを継承していません");
            }
            if (!typeof(ISchemaObjectControl).IsAssignableFrom(controlClass))
            {
                throw new ArgumentException("controlClassがISchemaObjectControlを実装していません");
            }
            _schemaObjectToControl[schemaObjectClass] = controlClass;
        }
        public static void UnregisterSchemaObjectControl(Type schemaObjectClass)
        {
            _schemaObjectToControl.Remove(schemaObjectClass);
        }
        protected Control NewControl(SchemaObject target)
        {
            Type t;
            if (!_schemaObjectToControl.TryGetValue(target.GetType(), out t))
            {
                return null;
            }
            ISchemaObjectControl ret = t.GetConstructor(new Type[0]).Invoke(null) as ISchemaObjectControl;
            if (ret == null)
            {
                return null;
            }
            ret.Target = target;
            target.Control = ret;
            return ret as Control;
        }

        private void UpdateSchema()
        {
            if (CurrentDataSet == null)
            {
                Title = TitleBase;
                treeViewItemTop.Header = "データベース";
                treeViewItemTop.Items.Clear();
                return;
            }
            Title = TitleBase + " - " + CurrentDataSet?.ConnectionInfo?.GetDefaultName();
            UpdateTreeViewDB();
            UpdateTabControlsTarget();
            gridLoading.Visibility = Visibility.Collapsed;
        }

        private Db2SourceContext _dataSetTemp;
        private void SetSchema()
        {
            CurrentDataSet = null;
            CurrentDataSet = _dataSetTemp;
        }

        private void CurrentDataSet_SchemaLoaded(object sender, EventArgs e)
        {
            _dataSetTemp = sender as Db2SourceContext;
            Dispatcher.Invoke(SetSchema, DispatcherPriority.Normal);
            SaveConnectionInfoToRegistry(_dataSetTemp.ConnectionInfo);
        }

        //#pragma warning disable 1998
        public async Task LoadSchemaAsync(Db2SourceContext dataSet)
        {
            try
            {
                await dataSet.LoadSchemaAsync();
                GC.Collect();
            }
            catch (Exception t)
            {
                App.HandleThreadException(t);
            }
            return;
        }

        public void LoadSchema(Db2SourceContext dataSet)
        {
            gridLoading.Visibility = Visibility.Visible;
            Task t = LoadSchemaAsync(dataSet);
        }

        //#pragma warning restore 1998

        private void OpenViewer(SchemaObject target)
        {
            ISchemaObjectControl curCtl = (tabControlMain.SelectedItem as TabItem)?.Content as ISchemaObjectControl;

            TabItem item = RequireTabItem(target, FindResource("TabItemStyleClosable") as Style);
            if (item == null)
            {
                return;
            }
            if (item.Parent == null)
            {
                tabControlMain.Items.Add(item);
            }
            tabControlMain.SelectedItem = item;
            ISchemaObjectControl newCtl = item.Content as ISchemaObjectControl;
            if (newCtl != null && curCtl != null)
            {
                newCtl.SelectedTabKey = curCtl.SelectedTabKey;
            }
        }

        protected void CurrentDataSetChanged(DependencyPropertyChangedEventArgs e)
        {
            if (CurrentDataSet == null)
            {
                return;
            }
            UpdateSchema();
            CurrentDataSet.SchemaObjectReplaced += CurrentDataSet_SchemaObjectReplaced;
            CurrentDataSet.Log += CurrentDataSet_Log;
            //CurrentDataSet.SchemaLoaded += CurrentDataSet_SchemaLoaded;
        }

        private void CurrentDataSet_Log(object sender, LogEventArgs e)
        {
            LogListBoxItem item = new LogListBoxItem();
            item.Time = DateTime.Now;
            item.Status = e.Status;
            item.Message = e.Text;
            item.ToolTip = e.Sql;
            listBoxLog.Items.Add(item);
            listBoxLog.SelectedItem = item;
            listBoxLog.ScrollIntoView(item);
            menuItemLogWindow.IsChecked = true;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == CurrentDataSetProperty)
            {
                CurrentDataSetChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        private SchemaObject GetTarget(TabItem item)
        {
            return (item.Content as ISchemaObjectControl)?.Target;
        }
        private void ReplaceSchemaObjectRecursive(TreeViewItem treeViewItem, SchemaObject newObj, SchemaObject oldObj)
        {
            foreach (TreeViewItem item in treeViewItem.Items)
            {
                ReplaceSchemaObjectRecursive(item, newObj, oldObj);
            }
            TreeNode node = treeViewItem.Tag as TreeNode;
            if (node == null)
            {
                return;
            }
            SchemaObject obj = node.Target as SchemaObject;
            if (obj == null)
            {
                return;
            }
            if (obj == oldObj)
            {
                if (newObj == null)
                {
                    TreeViewItem p = treeViewItem.Parent as TreeViewItem;
                    if (p != null)
                    {
                        p.Items.Remove(treeViewItem);
                    }
                }
                else
                {
                    treeViewItem.Tag = newObj;
                }
            }
        }
        private void CurrentDataSet_SchemaObjectReplaced(object sender, SchemaObjectReplacedEventArgs e)
        {
            foreach (TreeViewItem item in treeViewDB.Items)
            {
                ReplaceSchemaObjectRecursive(item, e.New, e.Old);
            }
            for (int i = tabControlMain.Items.Count - 1; 0 <= i; i--)
            {
                TabItem item = tabControlMain.Items[i] as TabItem;
                if (item == null)
                {
                    continue;
                }
                ISchemaObjectControl obj = (item.Content as ISchemaObjectControl);
                if (obj != null && obj.Target == e.Old)
                {
                    if (e.New == null)
                    {
                        tabControlMain.Items.RemoveAt(i);
                    }
                    else
                    {
                        obj.Target = e.New;
                    }
                }
            }
        }

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
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
            OpenViewer(obj);
        }

        private void TabItemCloseButton_Click(object sender, RoutedEventArgs e)
        {
            TabItem item = (sender as Control).TemplatedParent as TabItem;
            if (item == null)
            {
                return;
            }
            TabControl tab = item.Parent as TabControl;
            if (tab == null)
            {
                return;
            }
            tab.Items.Remove(item);
        }

        private void textBoxFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTreeView();
        }

        private void window_Initialized(object sender, EventArgs e)
        {
            UpdateMenuItemOpenDb();

            treeViewItemTop.Items.Clear();

            RegisterSchemaObjectControl(typeof(Table), typeof(TableControl));
            RegisterSchemaObjectControl(typeof(View), typeof(ViewControl));
            RegisterSchemaObjectControl(typeof(Sequence), typeof(SequenceControl));
            RegisterSchemaObjectControl(typeof(StoredFunction), typeof(StoredProcedureControl));
            RegisterSchemaObjectControl(typeof(ComplexType), typeof(ComplexTypeControl));
            TitleBase = Title;
        }

        private void menuItemRefreshSchema_Click(object sender, RoutedEventArgs e)
        {
            LoadSchema(CurrentDataSet);
        }

        private void buttonFilterKind_Click(object sender, RoutedEventArgs e)
        {
            buttonFilterKind.ContextMenu.IsOpen = true;
        }

        private void menuItemAddQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryControl c = new QueryControl();
            Binding b = new Binding("CurrentDataSet");
            b.ElementName = "window";
            c.SetBinding(QueryControl.CurrentDataSetProperty, b);
            TabItem item = NewTabItem(tabControlMain, "クエリ " + _queryControlIndex.ToString(), c, FindResource("TabItemStyleClosable") as Style);
            tabControlMain.SelectedItem = item;
            _queryControlIndex++;
        }

        private void buttonQuit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult ret = MessageBox.Show("終了します。よろしいですか?", "終了", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            Application.Current.Shutdown();
        }

        private void menuItemExportSchema_Click(object sender, RoutedEventArgs e)
        {
            ExportSchema win = new ExportSchema();
            win.Owner = this;
            win.FontFamily = FontFamily;
            win.FontSize = FontSize;
            win.FontStretch = FontStretch;
            win.FontStyle = FontStyle;
            win.FontWeight = FontWeight;
            win.DataSet = CurrentDataSet;
            win.ShowDialog();
        }

        private void LoadFromRegistry()
        {
            App.Registry.LoadStatus(this);

        }
        private void SaveToRegistry()
        {
            App.Registry.SaveStatus(this);
        }
        private ConnectionInfo NewConnectionInfoFromRegistry()
        {
            NpgsqlConnectionInfo info = new NpgsqlConnectionInfo()
            {
                ServerName = App.Registry.GetString("Connection", "ServerName", App.Hostname),
                ServerPort = App.Registry.GetInt32("Connection", "ServerPort", App.Port),
                DatabaseName = App.Registry.GetString("Connection", "DatabaseName", App.Database),
                UserName = App.Registry.GetString("Connection", "UserName", App.Username)
            };
            info.FillStoredPassword(false);
            return info;
        }
        private void SaveConnectionInfoToRegistry(ConnectionInfo info)
        {
            NpgsqlConnectionInfo obj = info as NpgsqlConnectionInfo;
            App.Registry.SetValue(0, "Connection", "ServerName", obj.ServerName);
            App.Registry.SetValue(0, "Connection", "ServerPort", obj.ServerPort);
            App.Registry.SetValue(0, "Connection", "DatabaseName", obj.DatabaseName);
            App.Registry.SetValue(0, "Connection", "UserName", obj.UserName);
        }
        private void Connect(ConnectionInfo info)
        {
            Db2SourceContext ds = info.NewDataSet();
            ds.SchemaLoaded += CurrentDataSet_SchemaLoaded;
            //_connectionToDataSet.Add(info, ds);
            LoadSchema(ds);
        }
        private string GetExecutableFromPath(string filename)
        {
            if (System.IO.Path.IsPathRooted(filename) && File.Exists(filename))
            {
                return filename;
            }
            string[] paths = Environment.GetEnvironmentVariable("PATH").Split(';');
            foreach (string dir in paths)
            {
                string path = System.IO.Path.Combine(dir, filename);
                if (File.Exists(path))
                {
                    return path;
                }
            }
            return null;
        }
        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            gridLoading.Visibility = Visibility.Collapsed;
            ConnectionInfo info = new NpgsqlConnectionInfo()
            {
                ServerName = App.Hostname,
                ServerPort = App.Port,
                DatabaseName = App.Database,
                UserName = App.Username
            };

            if (App.HasConnectionInfo)
            {
                if (!info.FillStoredPassword(true))
                {
                    Connect(info);
                    return;
                }
            }
            info = NewConnectionInfoFromRegistry();
            NewConnectionWindow win = new NewConnectionWindow();
            win.Owner = this;
            win.Target = info;
            bool? ret = win.ShowDialog();
            if (!ret.HasValue || !ret.Value)
            {
                return;
            }
            info = win.Target;
            Connect(info);
            App.Connections.Merge(info);
            App.Connections.Save();
            menuItemPsql.IsEnabled = !string.IsNullOrEmpty(GetExecutableFromPath(menuItemPsql.Tag.ToString()));
            menuItemPgdump.IsEnabled = !string.IsNullOrEmpty(GetExecutableFromPath(menuItemPgdump.Tag.ToString()));
        }

        private void menuItemPsql_Click(object sender, RoutedEventArgs e)
        {
            string exe = GetExecutableFromPath((sender as MenuItem).Tag.ToString());
            if (string.IsNullOrEmpty(exe))
            {
                return;
            }
            NpgsqlConnectionInfo info = CurrentDataSet.ConnectionInfo as NpgsqlConnectionInfo;
            string arg = string.Format("-h {0} -p {1} -d {2} -U {3}", info.ServerName, info.ServerPort, info.DatabaseName, info.UserName);
            Process.Start(exe, arg);
        }

        private void menuItemPgdump_Click(object sender, RoutedEventArgs e)
        {
            string exe = GetExecutableFromPath((sender as MenuItem).Tag.ToString());
            if (string.IsNullOrEmpty(exe))
            {
                return;
            }
            PgDumpOptionWindow win = new PgDumpOptionWindow();
            win.Owner = this;
            win.DataSet = CurrentDataSet;
            win.ShowDialog();
        }

        private void menuItemQueryHistory_Click(object sender, RoutedEventArgs e)
        {

        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            foreach (TabItem c in tabControlMain.Items)
            {
                ISchemaObjectControl obj = c.Content as ISchemaObjectControl;
                if (obj == null)
                {
                    continue;
                }
                bool f = e.Cancel;
                obj.OnTabClosing(sender, ref f);
                e.Cancel = f;
                if (e.Cancel)
                {
                    return;
                }
            }
        }

        private void menuItemOption_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuItemClearLog_Click(object sender, RoutedEventArgs e)
        {
            listBoxLog.Items.Clear();
            menuItemLogWindow.IsChecked = false;
        }

        private void menuItemEditConnections_Click(object sender, RoutedEventArgs e)
        {
            EditConnectionListWindow win = new EditConnectionListWindow();
            win.ShowDialog();
            UpdateMenuItemOpenDb();
        }

        private struct TabIndexRange
        {
            public ScrollViewer Viewer;
            public TabPanel Panel;
            /// <summary>
            /// 一部だけでも入っているTabIndexの最小値
            /// </summary>
            public int PartialMin;
            /// <summary>
            /// 完全に入っているTabIndexの最小値
            /// </summary>
            public int FullMin;
            /// <summary>
            /// 完全に入っているTabIndexの最大値
            /// </summary>
            public int FullMax;
            /// <summary>
            /// 一部だけでも入っているTabIndexの最大値
            /// </summary>
            public int PartialMax;
            public TabIndexRange(TabControl tabControl)
            {
                ControlTemplate tmpl = tabControl.Template as ControlTemplate;
                Viewer = tmpl.FindName("tabPanelScrollViewer", tabControl) as ScrollViewer;
                Panel = tmpl.FindName("headerPanel", tabControl) as TabPanel;
                PartialMin = int.MaxValue;
                FullMin = int.MaxValue;
                FullMax = -1;
                PartialMax = -1;
                int n = tabControl.Items.Count;
                for (int i = 0; i < n; i++)
                {
                    TabItem item = tabControl.Items[i] as TabItem;
                    Point[] p = new Point[] { new Point(0, 0), new Point(item.ActualWidth + 1, item.ActualHeight + 1) };
                    p[0] = item.TranslatePoint(p[0], Viewer);
                    p[1] = item.TranslatePoint(p[1], Viewer);
                    if (Viewer.ActualWidth <= p[1].X)
                    {
                        Normalize();
                        return;
                    }
                    if (p[0].X < Viewer.ActualWidth && 0 < p[1].X)
                    {
                        PartialMin = Math.Min(PartialMin, i);
                        PartialMax = i;
                    }
                    if (0 <= p[0].X && p[1].X < Viewer.ActualWidth)
                    {
                        FullMin = Math.Min(FullMin, i);
                        FullMax = i;
                    }
                }
                Normalize();
            }
            public void Normalize()
            {
                PartialMin = Math.Min(PartialMin, PartialMax);
                FullMin = Math.Min(FullMin, FullMax);
            }
        }

        private static void ScrollTabItemAsLeft(TabControl tabControl, ScrollViewer viewer, TabPanel panel, int index)
        {
            if (index < 0 || tabControl.Items.Count <= index)
            {
                return;
            }
            TabPanel pnl = tabControl.FindName("headerPanel") as TabPanel;
            TabItem item = tabControl.Items[index] as TabItem;
            Point p = item.TranslatePoint(new Point(), panel);
            viewer.ScrollToHorizontalOffset(p.X);
        }
        private static void ScrollTabItemAsRight(TabControl tabControl, ScrollViewer viewer, TabPanel panel, int index)
        {
            if (index < 0 || tabControl.Items.Count <= index)
            {
                return;
            }
            TabItem item = tabControl.Items[index] as TabItem;
            Point p1 = item.TranslatePoint(new Point(item.ActualWidth + 1, item.ActualHeight + 1), panel);
            double x0 = p1.X - viewer.ActualWidth;
            Point p0 = item.TranslatePoint(new Point(), panel);
            int i0 = index;
            for (int i = index - 1; 0 <= i; i--)
            {
                item = tabControl.Items[i] as TabItem;
                Point p = item.TranslatePoint(new Point(), panel);
                if (p.X < x0)
                {
                    break;
                }
                i0 = i;
                p0 = p;
                if (p.X == x0)
                {
                    break;
                }
            }
            viewer.ScrollToHorizontalOffset(p0.X);
        }
        private static void ScrollTabItemToVisible(TabControl tabControl)
        {
            TabIndexRange range = new TabIndexRange(tabControl);
            if (range.PartialMin == -1 || range.PartialMax == -1)
            {
                return;
            }
            int index = tabControl.SelectedIndex;
            if (range.FullMin <= index && index <= range.FullMax)
            {
                return;
            }
            if (index <= range.PartialMin)
            {
                ScrollTabItemAsLeft(tabControl, range.Viewer, range.Panel, index);
                return;
            }
            if (range.PartialMax <= index)
            {
                ScrollTabItemAsRight(tabControl, range.Viewer, range.Panel, index);
            }
            return;
        }
        private void ScrollTabItemToVisible()
        {
            ScrollTabItemToVisible(tabControlMain);
        }

        private void buttonScrollLeft_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (sender as Button);
            TabControl tab = btn.TemplatedParent as TabControl;
            TabIndexRange range = new TabIndexRange(tab);
            ScrollTabItemAsRight(tab, range.Viewer, range.Panel, range.FullMin);
            TabIndexRange range2 = new TabIndexRange(tab);
            if (0 < range.FullMin && range.FullMin == range2.FullMin)
            {
                ScrollTabItemAsRight(tab, range.Viewer, range.Panel, range.FullMin - 1);
            }
        }

        private void buttonScrollRight_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (sender as Button);
            TabControl tab = btn.TemplatedParent as TabControl;
            TabIndexRange range = new TabIndexRange(tab);
            ScrollTabItemAsLeft(tab, range.Viewer, range.Panel, range.FullMax);
            TabIndexRange range2 = new TabIndexRange(tab);
            if (range.FullMax < tab.Items.Count - 1 && range.FullMax == range2.FullMax)
            {
                ScrollTabItemAsLeft(tab, range.Viewer, range.Panel, range.FullMax + 1);
            }
        }

        private bool _isTabControlMainSelectionChanged = false;
        private void InvalidateTabControlMainSelection()
        {
            _isTabControlMainSelectionChanged = true;
        }
        private void UpdateTabControlMainSelection()
        {
            if (!_isTabControlMainSelectionChanged)
            {
                return;
            }
            _isTabControlMainSelectionChanged = false;
            ScrollTabItemToVisible();
        }

        private void tabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InvalidateTabControlMainSelection();
        }

        private void tabControlMain_LayoutUpdated(object sender, EventArgs e)
        {
            UpdateTabControlMainSelection();
        }

        private void UpdateButtonScrollSearchTabItemContextMenu(Button button)
        {
            ContextMenu menu = button.ContextMenu;
            menu.PlacementTarget = button;
            //menu.Placement = PlacementMode.Bottom;
            menu.Items.Clear();
            foreach (TabItem item in tabControlMain.Items)
            {
                MenuItem mi = new MenuItem()
                {
                    Header = item.Header,
                    Tag = item,
                };
                mi.Click += searchTabItemMenu_Click;
                menu.Items.Add(mi);
            }
        }
        private void buttonScrollSearchTabItem_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            UpdateButtonScrollSearchTabItemContextMenu(btn);
            btn.ContextMenu.IsOpen = true;
        }

        private void buttonScrollSearchTabItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            Button btn = sender as Button;
            UpdateButtonScrollSearchTabItemContextMenu(btn);
        }

        private void searchTabItemMenu_Click(object sender, RoutedEventArgs e)
        {
            tabControlMain.SelectedItem = (sender as MenuItem).Tag;
        }
    }
}
