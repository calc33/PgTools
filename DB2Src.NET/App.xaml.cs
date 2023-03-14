using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
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
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Windows.Themes;

namespace Db2Source
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App: Application
    {
        public static new App Current
        {
            get
            {
                return Application.Current as App;
            }
        }
        public static Db2SourceContext CurrentDataSet
        {
            get
            {
                return (Current?.MainWindow as MainWindow)?.CurrentDataSet;
            }
        }
        /// <summary>
        /// SQLを実行してエラーが出たらエラーメッセージをダイアログで表示する
        /// </summary>
        /// <param name="sql">実行したいSQL</param>
        /// <param name="forceDisconnect">SQL実行後に確実にセッションを切断したい場合はtrue</param>
        /// <returns>SQL実行に成功したらtrue, エラーが出たり実行できなかった場合はfalse</returns>
        public static bool ExecSql(string sql, bool forceDisconnect = false)
        {
            Db2SourceContext ds = CurrentDataSet;
            if (ds == null)
            {
                return false;
            }
            try
            {
                ds.ExecSql(sql, null, forceDisconnect);
            }
            catch (Exception t)
            {
                MessageBox.Show(Current.MainWindow, CurrentDataSet.GetExceptionMessage(t), Db2Source.Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                LogException(sql, t);
                return false;
            }
            return true;
        }
        /// <summary>
        /// 複数のSQLを順次実行してエラーが出たらエラーメッセージをダイアログで表示する
        /// エラーが出たら以降のSQLは実行しない
        /// </summary>
        /// <param name="sqls">実行したいSQLの配列</param>
        /// <param name="forceDisconnect">SQL実行後に確実にセッションを切断したい場合はtrue</param>
        /// <returns>SQL実行に成功したらtrue, エラーが出たり実行できなかった場合はfalse</returns>
        public static bool ExecSqls(string[] sqls, bool forceDisconnect = false)
        {
            Db2SourceContext ds = CurrentDataSet;
            if (ds == null)
            {
                return false;
            }
            foreach (string sql in sqls)
            {
                try
                {
                    ds.ExecSql(sql, null, forceDisconnect);
                }
                catch (Exception t)
                {
                    MessageBox.Show(Current.MainWindow, CurrentDataSet.GetExceptionMessage(t), Db2Source.Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    LogException(sql, t);
                    return false;
                }
            }
            return true;
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
            ISchemaObjectWpfControl ctrl = item.Content as ISchemaObjectWpfControl;
            bool cancel = false;
            ctrl?.OnTabClosing(sender, ref cancel);
            if (cancel)
            {
                return;
            }
            tab.Items.Remove(item);
            SchemaObject target = ctrl?.Target;
            ctrl?.OnTabClosed(sender);
            if (target != null)
            {
                target.Control = null;
            }
            (item as MovableTabItem)?.Dispose();
            DisposeDependencyObject(item);
            Dispatcher.InvokeAsync(() => { GC.Collect(); }, DispatcherPriority.ApplicationIdle);
            
        }
        public static void Log(string message)
        {
            Logger.Default.Log(message);
        }
        public static void LogException(Exception t)
        {
            Logger.Default.Log(t.ToString());
        }
        public static void LogException(string message, Exception t)
        {
            Logger.Default.Log(message + Environment.NewLine + t.ToString());
        }

        private static object _threadExceptionsLock = new object();
        private static List<Exception> _threadExceptions = new List<Exception>();
        public static RegistryFinder RegistryFinder { get; private set; } = new RegistryFinder();
        private static void ShowThreadException()
        {
            Exception t;
            while (0 < _threadExceptions.Count)
            {
                lock (_threadExceptionsLock)
                {
                    t = _threadExceptions[0];
                    _threadExceptions.RemoveAt(0);
                }
                MessageBox.Show(t.ToString(), Db2Source.Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static void HandleThreadException(Exception t)
        {
            lock (_threadExceptionsLock)
            {
                _threadExceptions.Add(t);
                LogException(t);
            }
            Current.Dispatcher.InvokeAsync(ShowThreadException);
        }
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), Db2Source.Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            LogException(e.Exception);
            e.Handled = true;
        }

        private static void ShowUsage()
        {
            MessageBox.Show(Db2Source.Properties.Resources.Usage, Db2Source.Properties.Resources.MessageBoxCaption_Info, MessageBoxButton.OK, MessageBoxImage.Information);
            Current.Shutdown();
        }

        private static int CompareByLastConnected(ConnectionInfo item1, ConnectionInfo item2)
        {
            int ret = -item1.LastConnected.CompareTo(item2.LastConnected);
            if (ret != 0)
            {
                return ret;
            }
            ret = -item1.ConnectionCount.CompareTo(item2.ConnectionCount);
            return ret;
        }
        private static NpgsqlConnectionInfo FindConnectionInfoByDbName(string dbname)
        {
            List<ConnectionInfo> l = new List<ConnectionInfo>(Connections);
            l.Sort(CompareByLastConnected);
            foreach (NpgsqlConnectionInfo info in l)
            {
                if (info.DatabaseName == dbname)
                {
                    return info;
                }
            }
            return null;
        }

        public static string GetDefaultLoginUserForDatabase(Database database)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }
            if (CurrentDataSet == null)
            {
                throw new ApplicationException("データベースに未接続のためユーザー情報が取得できません");
            }
            NpgsqlConnectionInfo obj = FindConnectionInfoByDbName(database.Name);
            if (obj != null)
            {
                return obj.UserName;
            }
            return database.DbaUserName;
        }

        public static void OpenDatabase(Database database, string userName, bool forceDialog)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }
            if (CurrentDataSet == null)
            {
                return;
            }
            string user = userName;
            if (string.IsNullOrEmpty(user))
            {
                user = GetDefaultLoginUserForDatabase(database);
            }
            NpgsqlConnectionInfo obj = CurrentDataSet.ConnectionInfo as NpgsqlConnectionInfo;
            string args = string.Format("-h {0} -p {1} -d {2}", obj.ServerName, obj.ServerPort, database.Name);
            if (!string.IsNullOrEmpty(user))
            {
                args += " -U " + user;
            }
            if (forceDialog)
            {
                args += " --connection-dialog";
            }
            string path = Assembly.GetExecutingAssembly().Location;
            Process.Start(path, args);
        }

        public static void OpenDatabase(NpgsqlConnectionInfo connectionInfo, bool forceDialog)
        {
            if (connectionInfo == null)
            {
                throw new ArgumentNullException("connectionInfo");
            }
            if (CurrentDataSet == null)
            {
                return;
            }
            //NpgsqlConnectionInfo info = connectionInfo as NpgsqlConnectionInfo;
            string args = string.Format("-h {0} -p {1} -d {2}", connectionInfo.ServerName, connectionInfo.ServerPort, connectionInfo.DatabaseName);
            if (!string.IsNullOrEmpty(connectionInfo.UserName))
            {
                args += " -U " + connectionInfo.UserName;
            }
            string path = Assembly.GetExecutingAssembly().Location;
            if (forceDialog)
            {
                args += " --connection-dialog";
            }
            Process.Start(path, args);
        }

        private static ConnectionList InitConnections()
        {
            ConnectionList.Register(typeof(NpgsqlConnectionInfo));
            return new ConnectionList();
        }
        private static ConnectionList _connections;
        public static ConnectionList Connections
        {
            get
            {
                if (_connections == null)
                {
                    _connections = InitConnections();
                }
                return _connections;
            }
        }

        public static readonly string DefaultHostname = "localhost";
        //public static readonly int DefaultPort = 5432;
        public static readonly string DefaultDatabase = "postgres";
        public static readonly string DefaultUsername = "postgres";
        private static string _hostname;
        private static int _port = 5432;
        private static string _database;
        private static string _username;

        private static bool HasFullyConnectionArgs()
        {
            return _hostname != null && _database != null && _username != null;
        }
        private static bool HasNoConnectionArgs()
        {
            return _hostname == null && _database == null && _username == null;
        }

        public static bool IsConnectedByArgs { get; private set; } = false;
        public static string Hostname { get { return _hostname ?? DefaultHostname; } set { _hostname = value; } }
        public static int Port { get { return _port; } set { _port = value; } }
        public static string Database { get { return _database ?? DefaultDatabase; } set { _database = value; } }
        public static string Username { get { return _username ?? DefaultUsername; } set { _username = value; } }
        public static bool ForceConnectionDialog { get; set; } = false;
        //public static string Password { get; private set; } = null;
        public static string SearchPath { get; private set; } = string.Empty;
        private static bool _showUsage = false;
        public static void AnalyzeArguments(string[] args)
        {
            try
            {
                int n = args.Length;
                for (int i = 0; i < n; i++)
                {
                    string a = args[i];
                    string v = null;
                    if (a.StartsWith("--"))
                    {
                        int p = a.IndexOf('=');
                        if (0 <= p)
                        {
                            v = a.Substring(p + 1);
                            a = a.Substring(0, p);
                        }
                    }
                    switch (a)
                    {
                        case "-h":
                            _hostname = args[++i];
                            break;
                        case "--host":
                            _hostname = v;
                            break;
                        case "-p":
                            _port = int.Parse(args[++i]);
                            break;
                        case "--port":
                            _port = int.Parse(v);
                            break;
                        case "-d":
                            _database = args[++i];
                            break;
                        case "--dbname":
                            _database = v;
                            break;
                        case "-U":
                            _username = args[++i];
                            break;
                        case "--username":
                            _username = v;
                            break;
                        case "-D":
                        case "--connection-dialog":
                            ForceConnectionDialog = true;
                            break;
                        case "-?":
                        case "--help":
                            _showUsage = true;
                            break;
                        default:
                            _showUsage = true;
                            break;
                    }
                }
            }
            catch
            {
                _showUsage = true;
            }
            if (_showUsage)
            {
                ShowUsage();
            }
            IsConnectedByArgs = HasFullyConnectionArgs();
            if (HasNoConnectionArgs())
            {
                RegistryBinding.Load(RegistryFinder);
            }
        }
        private static RegistryBinding InitRegistryBinding()
        {
            RegistryBinding binding = new RegistryBinding();
            binding.Register("Connection", "ServerName", typeof(App), "Hostname", new StringOperator());
            binding.Register("Connection", "ServerPort", typeof(App), "Port", new Int32Operator());
            binding.Register("Connection", "DatabaseName", typeof(App), "Database", new StringOperator());
            binding.Register("Connection", "UserName", typeof(App), "Username", new StringOperator());
            binding.Register("Connection", "SearchPath", typeof(App), "SearchPath", new StringOperator());
            return binding;
        }
        private static readonly RegistryBinding RegistryBinding = InitRegistryBinding();

        public static RegistryBinding NewRegistryBinding(ConnectionInfo info)
        {
            RegistryBinding binding = new RegistryBinding();
            NpgsqlConnectionInfo obj = info as NpgsqlConnectionInfo;
            binding.Register("Connection", "ServerName", obj, "ServerName", new StringOperator());
            binding.Register("Connection", "ServerPort", obj, "ServerPort", new Int32Operator());
            binding.Register("Connection", "DatabaseName", obj, "DatabaseName", new StringOperator());
            binding.Register("Connection", "UserName", obj, "UserName", new StringOperator());
            binding.Register("Connection", "SearchPath", obj, "SearchPath", new StringOperator());
            return binding;
        }

        private RegistryBinding SettingsBinding = new RegistryBinding();

        private void RegisterFontPackBinding(string path, FontPack fontPack)
        {
            SettingsBinding.Register(path, "FontFamily", fontPack, "FontFamilyName", new StringOperator());
            SettingsBinding.Register(path, "FontSize", fontPack, "FontSizeValue", new DoubleOperator());
            SettingsBinding.Register(path, "FontStretch", fontPack, "FontStretchName", new StringOperator());
            SettingsBinding.Register(path, "FontStyle", fontPack, "FontStyleName", new StringOperator());
            SettingsBinding.Register(path, "FontWeight", fontPack, "FontWeightName", new StringOperator());
        }

        private void InitSettingsFromRegistry()
        {
            RegisterFontPackBinding("BaseFont", FontPack.BaseFont);
            RegisterFontPackBinding("CodeFont", FontPack.CodeFont);
            RegisterFontPackBinding("TreeFont", FontPack.TreeFont);
            RegisterFontPackBinding("GridFont", FontPack.DataGridFont);
        }

        public void SaveSettingToRegistry()
        {
            SettingsBinding.Save(RegistryFinder);
        }

        private static bool TryConnect(ConnectionInfo info)
        {
            try
            {
                using (IDbConnection conn = info.NewConnection(true)) { }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private static ConnectionInfo NewConnectionInfoFromRegistry()
        {
            NpgsqlConnectionInfo info = new NpgsqlConnectionInfo()
            {
                ServerName = RegistryFinder.GetString("Connection", "ServerName", Hostname),
                ServerPort = RegistryFinder.GetInt32("Connection", "ServerPort", Port),
                DatabaseName = RegistryFinder.GetString("Connection", "DatabaseName", Database),
                UserName = RegistryFinder.GetString("Connection", "UserName", Username),
                SearchPath = RegistryFinder.GetString("Connection", "SearchPath", SearchPath),
            };
            info.FillStoredPassword(false);
            info = Connections.Find(info) as NpgsqlConnectionInfo;
            return info;
        }

        public static ConnectionInfo GetStartupConnection()
        {
            ConnectionInfo info = new NpgsqlConnectionInfo()
            {
                ServerName = Hostname,
                ServerPort = Port,
                DatabaseName = Database,
                UserName = Username
            };
            info = Connections.Merge(info);
            if (IsConnectedByArgs)
            {
                if (info.FillStoredPassword(true))
                {
                    if (!ForceConnectionDialog && TryConnect(info))
                    {
                        return info;
                    }
                }
            }
            NewConnectionWindow win = new NewConnectionWindow() { Target = info };
            bool? ret = win.ShowDialog();
            if (!ret.HasValue || !ret.Value)
            {
                return null;
            }
            return win.Target;
        }

        public static string GetExecutableFromPath(string filename)
        {
            if (Path.IsPathRooted(filename) && File.Exists(filename))
            {
                return filename;
            }
            string[] paths = Environment.GetEnvironmentVariable("PATH").Split(';');
            foreach (string dir in paths)
            {
                string path = Path.Combine(dir, filename);
                if (File.Exists(path))
                {
                    return path;
                }
            }
            return null;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InitSettingsFromRegistry();
            SettingsBinding.Load(RegistryFinder);
            Resources["DBNull"] = DBNull.Value;
            SQLTextBox.DefaultDecolations = Resources["SyntaxDecorationSettings"] as SyntaxDecorationCollection;
            AnalyzeArguments(e.Args);
            MainWindow window = new MainWindow() { StartupConnection = GetStartupConnection() };
            if (HasFullyConnectionArgs() && window.StartupConnection == null)
            {
                Shutdown();
                return;
            }
            window.Show();
            //MainWindow = window;    // 最初に作成したwindowが自動的にMainWindowになるため不要
        }

        private void GridSelectColumnButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            ScrollViewer sb = button.TemplatedParent as ScrollViewer;
            DataGrid grid = sb?.TemplatedParent as DataGrid;
            if (grid == null)
            {
                return;
            }
            SelectColumnWindow win = new SelectColumnWindow() { Owner = Window.GetWindow(grid), Grid = grid };
            win.Closed += SelectColumnWindow_Closed;
            WindowLocator.LocateNearby(button, win, NearbyLocation.DownRight);
            win.Show();
        }

        private void SelectColumnWindow_Closed(object sender, EventArgs e)
        {
            SelectColumnWindow win = sender as SelectColumnWindow;
            DataGrid grid = win.Grid;
            DataGridColumn col = win.SelectedColumn;
            object row = grid.CurrentItem;
            if (row == null && 0 < grid.Items.Count)
            {
                row = grid.Items[0];
            }
            if (row != null && col != null)
            {
                grid.ScrollIntoView(row, col);
            }
        }

        private void DataGridCheckBoxColumnHeaderStyleButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            DataGrid grid = FindVisualParent<DataGrid>(btn);
            ContextMenu menu = btn.ContextMenu;
            menu.Placement = PlacementMode.Bottom;
            //menu.PlacementTarget = VisualTreeHelper.GetParent(btn) as UIElement;
            menu.PlacementTarget = btn;
            menu.Tag = grid;
            menu.IsOpen = true;
        }

        public static T FindVisualParent<T>(DependencyObject item) where T : FrameworkElement
        {
            if (item == null)
            {
                return null;
            }
            if (item is T)
            {
                return (T)item;
            }
            DependencyObject obj;
            if (item is Visual)
            {
                obj = VisualTreeHelper.GetParent(item);
            }
            else
            {
                PropertyInfo prop = item.GetType().GetProperty("Parent");
                obj = prop?.GetValue(item) as DependencyObject;
            }
            while (obj != null)
            {
                if (obj is T)
                {
                    return (T)obj;
                }
                if (obj is Visual)
                {
                    obj = VisualTreeHelper.GetParent(obj);
                }
                else
                {
                    PropertyInfo prop = obj.GetType().GetProperty("Parent");
                    obj = prop?.GetValue(obj) as DependencyObject;
                }
            }
            return null;
        }

        public static T FindLogicalParent<T>(DependencyObject item) where T : DependencyObject
        {
            if (item == null)
            {
                return null;
            }
            if (item is T)
            {
                return (T)item;
            }
            DependencyObject obj = LogicalTreeHelper.GetParent(item);
            while (obj != null)
            {
                if (obj is T)
                {
                    return (T)obj;
                }
                obj = LogicalTreeHelper.GetParent(obj);
            }
            return null;
        }

        //public static T FindFirstVisualChild<T>(DependencyObject item) where T: DependencyObject
        //{
        //    int n = VisualTreeHelper.GetChildrenCount(item);
        //    List<DependencyObject> l = new List<DependencyObject>();
        //    for (int i = 0; i < n; i++)
        //    {
        //        DependencyObject obj = VisualTreeHelper.GetChild(item, i);
        //        if (obj is T)
        //        {
        //            return (T)obj;
        //        }
        //        l.Add(obj);
        //    }
        //    foreach (DependencyObject obj in l)
        //    {
        //        DependencyObject ret = FindFirstVisualChild<T>(obj);
        //        if (ret is T)
        //        {
        //            return (T)ret;
        //        }
        //    }
        //    return null;
        //}

        private static void UnlinkLogicalChildren(DependencyObject obj)
        {
            if (obj == null)
            {
                return;
            }
            Panel panel = obj as Panel;
            if (panel != null)
            {
                foreach (UIElement elem in panel.Children)
                {
                    UnlinkLogicalChildren(elem);
                }
                panel.Children.Clear();
            }
            ContentControl control = obj as ContentControl;
            if (control != null)
            {
                DependencyObject content = control.Content as DependencyObject;
                if (content != null)
                {
                    UnlinkLogicalChildren(content);
                }
                control.Content = null;
            }
            BindingOperations.ClearAllBindings(obj);
            LocalValueEnumerator enumerator = obj.GetLocalValueEnumerator();
            enumerator.Reset();
            while (enumerator.MoveNext())
            {
                DependencyProperty property = enumerator.Current.Property;
                if (property.ReadOnly)
                {
                    continue;
                }
                if (property.PropertyType.IsValueType)
                {
                    continue;
                }
                obj.SetValue(property, null);
            }
        }
        public static void DisposeDependencyObject(DependencyObject obj)
        {
            UnlinkLogicalChildren(obj);
        }

        private void DataGridCheckBoxColumnHeaderStyleBorder_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
            {
                return;
            }
            Button btn = sender as Button;
            ContextMenu menu = btn.ContextMenu;
            menu.Placement = PlacementMode.Bottom;
            menu.PlacementTarget = VisualTreeHelper.GetParent(btn) as UIElement;
            menu.IsOpen = true;
            e.Handled = true;
        }

        private void DataGridCheckBoxColumnHeaderStyleBorder_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            Button btn = sender as Button;
            DataGrid grid = FindVisualParent<DataGrid>(btn);
            ContextMenu menu = btn.ContextMenu;
            menu.Placement = PlacementMode.Bottom;
            menu.PlacementTarget = VisualTreeHelper.GetParent(btn) as UIElement;
            menu.Tag = grid;
        }

        private void MenuItemCheckAll_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menu = sender as MenuItem;
            DataGrid gr = (menu.Parent as ContextMenu)?.Tag as DataGrid;
            if (gr == null || gr.IsReadOnly)
            {
                return;
            }
            foreach (object item in gr.Items)
            {
                if (!(item is Row))
                {
                    continue;
                }
                ((Row)item).IsDeleted = true;
            }
        }

        private void MenuItemUncheckAll_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menu = sender as MenuItem;
            DataGrid gr = (menu.Parent as ContextMenu)?.Tag as DataGrid;
            if (gr == null || gr.IsReadOnly)
            {
                return;
            }
            foreach (object item in gr.Items)
            {
                if (!(item is Row))
                {
                    continue;
                }
                ((Row)item).IsDeleted = false;
            }
        }

        private void RevertColumnButton_Click(object sender, RoutedEventArgs e)
        {
            DataGridCell cell = FindVisualParent<DataGridCell>(sender as DependencyObject);
            Row row = cell.DataContext as Row;
            if (row == null || row.ChangeKind == ChangeKind.None)
            {
                return;
            }
            DataGrid grid = FindVisualParent<DataGrid>(sender as DependencyObject);
            grid.CommitEdit(DataGridEditingUnit.Row, true);
            row.RevertChanges();
        }
    }

    public class SqlLogger
    {
        internal StringBuilder Buffer { get; } = new StringBuilder();
        internal void Log(object sender, LogEventArgs e)
        {
            Buffer.AppendLine(e.Text);
        }
    }

    public class NewNpgsqlConnectionInfo: NpgsqlConnectionInfo
    {
        public NewNpgsqlConnectionInfo(bool fromSetting) : base()
        {
            if (fromSetting)
            {
                ServerName = App.Hostname;
                ServerPort = App.Port;
                DatabaseName = App.Database;
                UserName = App.Username;
            }
            Name = Properties.Resources.NewConnectionTitle;
        }
    }

    public interface ISchemaObjectWpfControl : ISchemaObjectControl
    {
        DependencyObject Parent { get; }
    }

    public class InvertBooleanConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }
    public class InvertBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class IsEnabledToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value != null && (bool)value) ? SystemColors.ControlTextBrush : SystemColors.GrayTextBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ItemsSourceToColumnFilterButtonVisibilityConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ICollection source = value as ICollection;
            if (source != null && 0 < source.Count)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ColorToBrushConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Color))
            {
                return Brushes.Transparent;
            }
            return new SolidColorBrush((Color)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is SolidColorBrush))
            {
                return Colors.Transparent;
            }
            return ((SolidColorBrush)value).Color;
        }
    }
    public class RGBToBrushConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is RGB))
            {
                return Brushes.Transparent;
            }
            RGB rgb = (RGB)value;
            return new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NullableIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            try
            {
                if (value is string)
                {
                    string v = (string)value;
                    if (string.IsNullOrEmpty(v))
                    {
                        return null;
                    }
                    return int.Parse(v);
                }
                return System.Convert.ToInt32(value);
            }
            catch (FormatException)
            {
                return new ValidationResult(false, value);
            }
            catch (InvalidCastException)
            {
                return new ValidationResult(false, value);
            }
            catch (OverflowException)
            {
                return new ValidationResult(false, value);
            }
        }
    }

    public class NotNullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value != null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotNullOrEmptyToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }
            if (value is string)
            {
                return !string.IsNullOrEmpty(((string)value).Trim());
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public interface IRegistryStore
    {
        void LoadFromRegistry();
        void SaveToRegistry();
    }
}