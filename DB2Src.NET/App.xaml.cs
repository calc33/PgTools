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
                MessageBox.Show(Current.MainWindow, App.CurrentDataSet.GetExceptionMessage(t), Db2Source.Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show(Current.MainWindow, App.CurrentDataSet.GetExceptionMessage(t), Db2Source.Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
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
            ctrl?.OnTabClosed(sender);
        }

        private static readonly object LogLock = new object();
        internal class LogWriter
        {
            private static readonly string LogDir = Path.Combine(Db2SourceContext.AppDataDir, "Log");
            private static readonly string LockPath = Path.Combine(Db2SourceContext.AppDataDir, "log_lock");
            private string LogPath;
            private string Message;
            private LogWriter(string message)
            {
                DateTime dt = DateTime.Now;
                LogPath = Path.Combine(LogDir, string.Format("Log{0:yyyyMMdd}.txt", dt));
                Message = string.Format("[{0:HH:mm:ss}] {1}", dt, message);
            }

            private void DoExecute()
            {
                FileStream lockStream = null;
                while (lockStream == null)
                {
                    try
                    {
                        lockStream = new FileStream(LockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(0);
                    }
                    catch { throw; }
                }
                try
                {
                    Directory.CreateDirectory(LogDir);
                    using (StreamWriter writer = new StreamWriter(LogPath, true, Encoding.UTF8))
                    {
                        writer.WriteLine(Message);
                        writer.Flush();
                    }
                }
                finally
                {
                    lockStream.Close();
                    lockStream.Dispose();
                }
            }

            private void Execute()
            {
                Task t = Task.Run(new Action(DoExecute));
            }
            public static void Log(string message)
            {
                LogWriter writer = new LogWriter(message);
                writer.Execute();
            }
        }
        public static void Log(string message)
        {
            LogWriter.Log(message);
        }
        public static void LogException(Exception t)
        {
            LogWriter.Log(t.ToString());
        }
        public static void LogException(string message, Exception t)
        {
            LogWriter.Log(message + Environment.NewLine + t.ToString());
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
            Current.Dispatcher.Invoke(ShowThreadException);
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
            App.Current.Shutdown();
        }

        public static void OpenDatabase(Database database)
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

        public static bool HasConnectionInfo { get; private set; } = false;
        public static string Hostname { get; private set; } = "localhost";
        public static int Port { get; private set; } = 5432;
        public static string Database { get; private set; } = "postgres";
        public static string Username { get; private set; } = "postgres";
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
                            Hostname = args[++i];
                            HasConnectionInfo = true;
                            break;
                        case "--host":
                            Hostname = v;
                            HasConnectionInfo = true;
                            break;
                        case "-p":
                            Port = int.Parse(args[++i]);
                            HasConnectionInfo = true;
                            break;
                        case "--port":
                            Port = int.Parse(v);
                            HasConnectionInfo = true;
                            break;
                        case "-d":
                            Database = args[++i];
                            HasConnectionInfo = true;
                            break;
                        case "--dbname":
                            Database = v;
                            HasConnectionInfo = true;
                            break;
                        case "-U":
                            Username = args[++i];
                            HasConnectionInfo = true;
                            break;
                        case "--username":
                            Username = v;
                            HasConnectionInfo = true;
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
            if (!HasConnectionInfo)
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

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            InitSettingsFromRegistry();
            SettingsBinding.Load(RegistryFinder);
            Resources["DBNull"] = DBNull.Value;
            AnalyzeArguments(e.Args);
        }

        public static void CopyFont(Control destination, Control source)
        {
            destination.FontFamily = source.FontFamily;
            destination.FontSize = source.FontSize;
            destination.FontStretch = source.FontStretch;
            destination.FontStyle = source.FontStyle;
            destination.FontWeight = source.FontWeight;
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
            SelectColumnWindow win = new SelectColumnWindow();
            win.Owner = Window.GetWindow(grid);
            CopyFont(win, win.Owner);
            win.Closed += SelectColumnWindow_Closed;
            win.Grid = grid;
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

    public interface IRegistryStore
    {
        void LoadFromRegistry();
        void SaveToRegistry();
    }
}