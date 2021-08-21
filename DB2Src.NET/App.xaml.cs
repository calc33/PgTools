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
                Task t = Task.Run(DoExecute);
            }
            public static void Log(string message)
            {
                LogWriter writer = new LogWriter(message);
                writer.Execute();
            }
        }
        private static void LogException(Exception t)
        {
            LogWriter.Log(t.ToString());
        }

        private static object _threadExceptionsLock = new object();
        private static List<Exception> _threadExceptions = new List<Exception>();
        public static RegistryManager Registry { get; private set; } = new RegistryManager(@"HKCU\Software\DB2Source", @"HKLM\Software\DB2Source");
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
                MessageBox.Show(t.ToString(), "エラー1", MessageBoxButton.OK, MessageBoxImage.Error);
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
            MessageBox.Show(e.Exception.ToString(), "エラー2", MessageBoxButton.OK, MessageBoxImage.Error);
            LogException(e.Exception);
            e.Handled = true;
        }

        private static void ShowUsage()
        {
            MessageBox.Show(Db2Source.Properties.Resources.Usage, "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            App.Current.Shutdown();
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
                LoadConnectionInfoFromRegistry();
            }
        }
        public static void LoadConnectionInfoFromRegistry()
        {
            Hostname = Registry.GetString("Connection", "ServerName", Hostname);
            Port = Registry.GetInt32("Connection", "ServerPort", Port);
            Database = Registry.GetString("Connection", "DatabaseName", Database);
            Username = Registry.GetString("Connection", "UserName", Username);
            SearchPath = Registry.GetString("Connection", "SearchPath", SearchPath);
        }

        public static void SaveConnectionInfoToRegistry(ConnectionInfo info)
        {
            NpgsqlConnectionInfo obj = info as NpgsqlConnectionInfo;
            Registry.SetValue(0, "Connection", "ServerName", obj.ServerName ?? string.Empty);
            Registry.SetValue(0, "Connection", "ServerPort", obj.ServerPort);
            Registry.SetValue(0, "Connection", "DatabaseName", obj.DatabaseName ?? string.Empty);
            Registry.SetValue(0, "Connection", "UserName", obj.UserName ?? string.Empty);
            Registry.SetValue(0, "Connection", "SearchPath", obj.SearchPath ?? string.Empty);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Resources["DBNull"] = DBNull.Value;
            AnalyzeArguments(e.Args);
        }

        private static Rect GetWorkingAreaOf(FrameworkElement element)
        {
            Point p = element.PointToScreen(new Point());
            System.Windows.Forms.Screen sc = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)p.X, (int)p.Y));
            return new Rect(sc.WorkingArea.X, sc.WorkingArea.Y, sc.WorkingArea.Width, sc.WorkingArea.Height);
        }

        private static Point LocateNearby(Rect elementRect, Rect windowRect, NearbyLocation location)
        {
            switch (location)
            {
                case NearbyLocation.DownLeft:
                    return new Point(elementRect.Left - windowRect.Left, elementRect.Bottom - windowRect.Top);
                case NearbyLocation.DownRight:
                    return new Point(elementRect.Right - windowRect.Right, elementRect.Bottom - windowRect.Top);
                case NearbyLocation.UpLeft:
                    return new Point(elementRect.Left - windowRect.Left, elementRect.Top - windowRect.Bottom);
                case NearbyLocation.UpRight:
                    return new Point(elementRect.Right - windowRect.Right, elementRect.Top - windowRect.Bottom);
                case NearbyLocation.LeftSideTop:
                    return new Point(elementRect.Left - windowRect.Right, elementRect.Top - windowRect.Top);
                case NearbyLocation.LeftSideBottom:
                    return new Point(elementRect.Left - windowRect.Right, elementRect.Bottom - windowRect.Bottom);
                case NearbyLocation.RightSideTop:
                    return new Point(elementRect.Right - windowRect.Left, elementRect.Top - windowRect.Top);
                case NearbyLocation.RightSideBottom:
                    return new Point(elementRect.Right - windowRect.Left, elementRect.Bottom - windowRect.Bottom);
                default:
                    return new Point(elementRect.Left - windowRect.Left, elementRect.Bottom - windowRect.Top);
            }
        }

        private static readonly Thickness ResizeDelta = new Thickness(10, 0, 15, 0);
        private static Rect GetWindowRect(Window window, Size widthSize, Thickness margin)
        {
            Rect r = new Rect(margin.Left, margin.Top, widthSize.Width + margin.Right, widthSize.Height + margin.Bottom);
            if (window.ResizeMode == ResizeMode.CanResize || window.ResizeMode == ResizeMode.CanResizeWithGrip)
            {
                r.X += ResizeDelta.Left;
                r.Y += ResizeDelta.Top;
                r.Width -= ResizeDelta.Right;
                r.Height -= ResizeDelta.Bottom;
            }
            return r;
        }
        private static readonly Dictionary<NearbyLocation, NearbyLocation[]> NearbyLocationCandidates = new Dictionary<NearbyLocation, NearbyLocation[]>()
        {
            { NearbyLocation.DownLeft, new NearbyLocation[] { NearbyLocation.DownRight, NearbyLocation.UpLeft, NearbyLocation.UpRight } },
            { NearbyLocation.DownRight, new NearbyLocation[] { NearbyLocation.DownLeft, NearbyLocation.UpRight, NearbyLocation.UpLeft } },
            { NearbyLocation.UpLeft, new NearbyLocation[] { NearbyLocation.UpRight, NearbyLocation.DownLeft, NearbyLocation.DownRight } },
            { NearbyLocation.UpRight, new NearbyLocation[] { NearbyLocation.UpLeft, NearbyLocation.DownRight, NearbyLocation.DownLeft } },
            { NearbyLocation.LeftSideTop, new NearbyLocation[] { NearbyLocation.LeftSideBottom, NearbyLocation.RightSideTop, NearbyLocation.RightSideBottom } },
            { NearbyLocation.LeftSideBottom, new NearbyLocation[] { NearbyLocation.LeftSideTop, NearbyLocation.RightSideBottom, NearbyLocation.RightSideTop } },
            { NearbyLocation.RightSideTop, new NearbyLocation[] { NearbyLocation.RightSideBottom, NearbyLocation.LeftSideTop, NearbyLocation.LeftSideBottom } },
            { NearbyLocation.RightSideBottom, new NearbyLocation[] { NearbyLocation.RightSideTop, NearbyLocation.LeftSideBottom, NearbyLocation.LeftSideTop } },
        };
        private static Rect TryLocate(Window window, Size windowSize, Thickness margin, Rect elementRect, NearbyLocation location, Rect workingArea)
        {
            Rect rW = GetWindowRect(window, windowSize, margin);
            Rect r0 = new Rect(LocateNearby(elementRect, rW, location), windowSize);
            if (workingArea.Contains(r0))
            {
                return r0;
            }
            foreach (NearbyLocation l in NearbyLocationCandidates[location])
            {
                Rect r = new Rect(LocateNearby(elementRect, rW, l), windowSize);
                if (workingArea.Contains(r))
                {
                    return r;
                }
            }
            if (workingArea.Right < r0.Right)
            {
                r0.X += (workingArea.Right - r0.Right);
            }
            if (workingArea.Bottom < r0.Bottom)
            {
                r0.Y += (workingArea.Bottom - r0.Bottom);
            }
            if (r0.Left < workingArea.Left)
            {
                r0.X = workingArea.Left;
            }
            if (r0.Top < workingArea.Top)
            {
                r0.Y = workingArea.Top;
            }
            return r0;
        }
        public static void ShowNearby(Window window, FrameworkElement element, NearbyLocation location)
        {
            WindowLocator.LocateNearby(element, window, location);
            window.Show();
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
            win.Closed += SelectColumnWindow_Closed;
            win.Grid = grid;
            ShowNearby(win, button, NearbyLocation.DownRight);
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
        public const string DEFAULE_NAME = "新しい接続...";
        public NewNpgsqlConnectionInfo(bool fromSetting) : base()
        {
            if (fromSetting)
            {
                ServerName = App.Hostname;
                ServerPort = App.Port;
                DatabaseName = App.Database;
                UserName = App.Username;
            }
            Name = DEFAULE_NAME;
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
}