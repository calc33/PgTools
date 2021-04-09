using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
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

        private DataGrid FindDataGridRecursive(DependencyObject obj)
        {
            if (obj == null)
            {
                return null;
            }
            if (obj is DataGrid)
            {
                DataGrid gr = (DataGrid)obj;
                return gr.IsVisible ? gr : null;
            }
            if (obj is TabControl)
            {
                return FindDataGridRecursive(((TabControl)obj).SelectedItem as TabItem);
            }
            if (obj is ContentControl)
            {
                return FindDataGridRecursive(((ContentControl)obj).Content as DependencyObject);
            }
            if (obj is Panel)
            {
                foreach (object elem in LogicalTreeHelper.GetChildren(obj))
                {
                    if (!(elem is DependencyObject))
                    {
                        return null;
                    }
                    DataGrid ret = FindDataGridRecursive((DependencyObject)elem);
                    if (ret != null)
                    {
                        return ret;
                    }
                }
                return null;
            }
            return null;
        }

        private DataGrid GetActiveDataGrid()
        {
            FrameworkElement elem = tabControlMain.SelectedItem as FrameworkElement;
            if (elem == null)
            {
                return null;
            }
            return FindDataGridRecursive(tabControlMain);
        }

        private static CommandBinding FindCommandBinding(DataGrid grid, ICommand command)
        {
            if (grid == null)
            {
                return null;
            }
            foreach (CommandBinding b in grid.CommandBindings)
            {
                if (b.Command == command)
                {
                    return b;
                }
            }
            return null;
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
            MovableTabItem item = new MovableTabItem();
            item.Content = element;
            item.Header = new TextBlock() { Text = header };
            item.Style = tabItemStyle;
            parent.Items.Add(item);
            return item;
        }

        private object TabItemLock = new object();
        public TabItem RequireTabItem(SchemaObject target, Style tabItemStyle)
        {
            ISchemaObjectWpfControl ctrl = target.Control as ISchemaObjectWpfControl;
            if (ctrl != null)
            {
                ctrl.Target = CurrentDataSet.Refresh(target);
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
                TabItem item = NewTabItem(tabControlMain, target.FullName, ctrl as UIElement, tabItemStyle);
                item.Tag = target;
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
            if (!typeof(ISchemaObjectWpfControl).IsAssignableFrom(controlClass))
            {
                throw new ArgumentException("controlClassがIWpfSchemaObjectControlを実装していません");
            }
            _schemaObjectToControl[schemaObjectClass] = controlClass;
        }
        public static void UnregisterSchemaObjectControl(Type schemaObjectClass)
        {
            _schemaObjectToControl.Remove(schemaObjectClass);
        }
        
        protected ISchemaObjectWpfControl NewControl(SchemaObject target)
        {
            Type t;
            if (!_schemaObjectToControl.TryGetValue(target.GetType(), out t))
            {
                return null;
            }
            ISchemaObjectWpfControl ret = t.GetConstructor(new Type[0]).Invoke(null) as ISchemaObjectWpfControl;
            if (ret == null)
            {
                return null;
            }
            ret.Target = target;
            target.Control = ret;
            return ret;
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

        public static Color ToColor(RGB rgb)
        {
            return Color.FromRgb(rgb.R, rgb.G, rgb.B);
        }
        private Brush GetBackgroundColor()
        {
            if (CurrentDataSet == null || CurrentDataSet.ConnectionInfo == null)
            {
                return SystemColors.ControlBrush;
            }
            return new SolidColorBrush(ToColor(CurrentDataSet.ConnectionInfo.BackgroundColor));
        }

        private Db2SourceContext _dataSetTemp;
        private void SetSchema()
        {
            CurrentDataSet = null;
            CurrentDataSet = _dataSetTemp;
            Resources["WindowBackground"] = GetBackgroundColor();
        }

        private void CurrentDataSet_SchemaLoaded(object sender, EventArgs e)
        {
            _dataSetTemp = sender as Db2SourceContext;
            Dispatcher.Invoke(SetSchema, DispatcherPriority.Normal);
            SaveConnectionInfoToRegistry(_dataSetTemp.ConnectionInfo);
        }

        //#pragma warning disable 1998
        public async Task LoadSchemaAsync(Db2SourceContext dataSet, IDbConnection connection)
        {
            try
            {
                await dataSet.LoadSchemaAsync(connection);
                GC.Collect();
            }
            catch (Exception t)
            {
                App.HandleThreadException(t);
            }
            finally
            {
                connection.Dispose();
            }
            return;
        }
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

        public void LoadSchema(Db2SourceContext dataSet, IDbConnection connection)
        {
            gridLoading.Visibility = Visibility.Visible;
            Task t = LoadSchemaAsync(dataSet, connection);
        }
        public void LoadSchema(Db2SourceContext dataSet)
        {
            gridLoading.Visibility = Visibility.Visible;
            Task t = LoadSchemaAsync(dataSet);
        }

        //#pragma warning restore 1998

        private void OpenViewer(SchemaObject target)
        {
            ISchemaObjectWpfControl curCtl = (tabControlMain.SelectedItem as TabItem)?.Content as ISchemaObjectWpfControl;

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
            ISchemaObjectWpfControl newCtl = item.Content as ISchemaObjectWpfControl;
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
            return (item.Content as ISchemaObjectWpfControl)?.Target;
        }
        private void ReplaceSchemaObjectRecursive(TreeViewItem treeViewItem, SchemaObject newObj, SchemaObject oldObj)
        {
            for (int i = treeViewItem.Items.Count - 1; 0 <= i; i--)
            {
                TreeViewItem item = treeViewItem.Items[i] as TreeViewItem;
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
                ISchemaObjectWpfControl obj = (item.Content as ISchemaObjectWpfControl);
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
            // フィルタ文字列が空文字列になった時はEnterを押さないとツリーが更新されない
            if (!string.IsNullOrEmpty(_treeViewFilterText) && string.IsNullOrEmpty(textBoxFilter.Text))
            {
                return;
            }
            FilterTreeView(true);
        }

        private void textBoxFilter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FilterTreeView(false);
            }
        }

        private void textBoxFilter_LostFocus(object sender, RoutedEventArgs e)
        {
            FilterTreeView(false);
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
            RegisterSchemaObjectControl(typeof(PgsqlBasicType), typeof(PgsqlTypeControl));
            RegisterSchemaObjectControl(typeof(PgsqlEnumType), typeof(PgsqlTypeControl));
            RegisterSchemaObjectControl(typeof(PgsqlRangeType), typeof(PgsqlTypeControl));
            RegisterSchemaObjectControl(typeof(PgsqlDatabase), typeof(DatabaseControl));
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
            App.Connections.FillPassword(info);
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
            IDbConnection conn = info.NewConnection(true);
            ds.SchemaLoaded += CurrentDataSet_SchemaLoaded;
            LoadSchema(ds, conn);
        }
        private bool TryConnect(ConnectionInfo info)
        {
            Db2SourceContext ds = info.NewDataSet();
            IDbConnection conn = null;
            try
            {
                conn = info.NewConnection(true);
            }
            catch
            {
                return false;
            }
            ds.SchemaLoaded += CurrentDataSet_SchemaLoaded;
            LoadSchema(ds, conn);
            return true;
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
                    if (TryConnect(info))
                    {
                        return;
                    }
                }
            }
            else
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
            menuItemPsql.IsEnabled = !string.IsNullOrEmpty(GetExecutableFromPath(menuItemPsql.Tag.ToString()));
            menuItemPgdump.IsEnabled = !string.IsNullOrEmpty(GetExecutableFromPath(menuItemPgdump.Tag.ToString()));
            {
                CommandBinding cb;
                cb = new CommandBinding(DataGridCommands.CopyTable, CopyTableCommand_Executed, CopyTableCommand_CanExecute);
                CommandBindings.Add(cb);
                cb = new CommandBinding(DataGridCommands.CopyTableContent, CopyTableContentCommand_Executed, CopyTableCommand_CanExecute);
                CommandBindings.Add(cb);
                cb = new CommandBinding(DataGridCommands.CopyTableAsInsert, CopyTableAsInsertCommand_Executed, CopyTableCommand_CanExecute);
                CommandBindings.Add(cb);
                cb = new CommandBinding(DataGridCommands.CopyTableAsCopy, CopyTableAsCopyCommand_Executed, CopyTableCommand_CanExecute);
                CommandBindings.Add(cb);
            }

        }

        private void CopyTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.OriginalSource is DataGrid)
            {
                return;
            }
            DataGrid gr = GetActiveDataGrid();
            e.CanExecute = (gr != null) && DataGridCommands.CopyTable.CanExecute(null, gr);
            e.Handled = true;
        }
        private void CopyTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is DataGrid)
            {
                return;
            }
            DataGrid gr = GetActiveDataGrid();
            if (gr == null)
            {
                return;
            }
            DataGridCommands.CopyTable.Execute(null, gr);
            e.Handled = true;
        }


        private void CopyTableContentCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is DataGrid)
            {
                return;
            }
            DataGrid gr = GetActiveDataGrid();
            if (gr == null)
            {
                return;
            }
            DataGridCommands.CopyTableContent.Execute(null, gr);
            e.Handled = true;
        }

        private void CopyTableAsInsertCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is DataGrid)
            {
                return;
            }
            DataGrid gr = GetActiveDataGrid();
            if (gr == null)
            {
                return;
            }
            DataGridCommands.CopyTableAsInsert.Execute(null, gr);
            e.Handled = true;
        }

        private void CopyTableAsCopyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is DataGrid)
            {
                return;
            }
            DataGrid gr = GetActiveDataGrid();
            if (gr == null)
            {
                return;
            }
            DataGridCommands.CopyTableAsCopy.Execute(null, gr);
            e.Handled = true;
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
            win.Show();
            //win.ShowDialog();
        }

        private void menuItemQueryHistory_Click(object sender, RoutedEventArgs e)
        {

        }

        private void window_Closing(object sender, CancelEventArgs e)
        {
            foreach (TabItem c in tabControlMain.Items)
            {
                ISchemaObjectWpfControl obj = c.Content as ISchemaObjectWpfControl;
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

        private void buttonScrollSearchTabItem_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            SelectTabItemWindow win = new SelectTabItemWindow();
            win.TabControl = tabControlMain;
            win.Closed += SelectTabItemWindow_Closed;
            App.ShowNearby(win, btn, NearbyLocation.DownRight);
        }

        private void SelectTabItemWindow_Closed(object sender, EventArgs e)
        {
            SelectTabItemWindow win = sender as SelectTabItemWindow;
            TabItem sel = win.SelectedItem;
            if (sel == null)
            {
                return;
            }
            tabControlMain.SelectedItem = sel;
        }

        private MovableTabItem _movingTabItem = null;
        internal MovableTabItem MovingTabItem
        {
            get { return _movingTabItem; }
            set
            {
                if (_movingTabItem == value)
                {
                    return;
                }
                if (_movingTabItem != null)
                {
                    _movingTabItem.IsMoving = false;
                }
                _movingTabItem = value;
                if (_movingTabItem != null)
                {
                    _movingTabItem.IsMoving = true;
                }
            }
        }

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (MovingTabItem != null)
            {
                if (e.MouseDevice.LeftButton == MouseButtonState.Released)
                {
                    MovingTabItem = null;
                }
                return;
            }
            MovableTabItem item = e.Source as MovableTabItem;
            if (item == null)
            {
                return;
            }
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
            {
                ScrollViewer sv = App.FindVisualParent<ScrollViewer>(item);
                if (sv != null && !sv.IsMouseCaptured)
                {
                    Mouse.Capture(sv);
                }
                MovingTabItem = item;
            }
        }

        private void MoveTabItem(TabItem goal)
        {
            if (MovingTabItem == null)
            {
                return;
            }
            if (MovingTabItem == goal)
            {
                return;
            }
            TabControl tab = MovingTabItem.Parent as TabControl;
            int goalPos = tab.Items.IndexOf(goal);
            int i = tab.Items.IndexOf(MovingTabItem);
            bool sel = MovingTabItem.IsSelected;
            tab.Items.RemoveAt(i);
            tab.Items.Insert(goalPos, MovingTabItem);
            if (sel)
            {
                tab.SelectedItem = MovingTabItem;
            }
        }
        private void tabPanelScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (MovingTabItem == null)
            {
                return;
            }

            ScrollViewer viewer = sender as ScrollViewer;
            if (viewer == null)
            {
                return;
            }
            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed && !viewer.IsMouseCaptured)
            {
                viewer.CaptureMouse();
            }
            FrameworkElement parent = VisualTreeHelper.GetParent(viewer) as FrameworkElement;
            Point p = e.MouseDevice.GetPosition(parent);
            double dx = 0;
            if (p.X < 0)
            {
                dx = p.X;
            }
            else if (viewer.ActualWidth < p.X)
            {
                dx = p.X - viewer.ActualWidth;
            }
            TabControl tab = MovingTabItem.Parent as TabControl;
            MovableTabItem goal;
            if (dx == 0)
            {
                HitTestResult ret = VisualTreeHelper.HitTest(viewer, e.MouseDevice.GetPosition(viewer));
                if (ret == null)
                {
                    return;
                }
                goal = App.FindVisualParent<MovableTabItem>(ret.VisualHit);
                double x = e.MouseDevice.GetPosition(goal).X;
                if (x < 0 || MovingTabItem.ActualWidth < x)
                {
                    // 移動した結果マウスの位置が選択しているタブの範囲外になって振動を起こさないように
                    goal = null;
                }
            }
            else if (dx < 0)
            {
                TabIndexRange range = new TabIndexRange(tab);
                goal = tab.Items[Math.Max(0, range.PartialMin - 1)] as MovableTabItem;
            }
            else
            {
                TabIndexRange range = new TabIndexRange(tab);
                goal = tab.Items[Math.Min(range.PartialMax + 1, tab.Items.Count - 1)] as MovableTabItem;
            }
            if (goal == null || goal.Parent != MovingTabItem.Parent)
            {
                return;
            }
            MoveTabItem(goal);
        }

        private void tabPanelScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer viewer = sender as ScrollViewer;
            if (viewer != null && viewer.IsMouseCaptured)
            {
                viewer.ReleaseMouseCapture();
            }
            MovingTabItem = null;
        }

        private void tabPanelScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            MovingTabItem = null;
            ScrollViewer viewer = sender as ScrollViewer;
            HitTestResult ret = VisualTreeHelper.HitTest(viewer, e.MouseDevice.GetPosition(viewer));
            if (ret == null)
            {
                return;
            }
            Control c = App.FindVisualParent<Control>(ret.VisualHit);
            MovingTabItem = c as MovableTabItem;
        }
    }
    public class MovableTabItem: TabItem
    {
        public static readonly DependencyProperty IsMovingProperty = DependencyProperty.Register("IsMoving", typeof(bool), typeof(MovableTabItem));
        public bool IsMoving
        {
            get
            {
                return (bool)GetValue(IsMovingProperty);
            }
            set
            {
                SetValue(IsMovingProperty, value);
            }
        }
    }
    public class RGBToColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RGB rgb = (RGB)value;
            return new SolidColorBrush(MainWindow.ToColor(rgb));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color c = ((SolidColorBrush)value).Color;
            return new RGB(c.R, c.G, c.B);
        }
    }
}
