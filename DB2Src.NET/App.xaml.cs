using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

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
            ISchemaObjectControl ctrl = item.Content as ISchemaObjectControl;
            bool cancel = false;
            ctrl?.OnTabClosing(sender, ref cancel);
            if (cancel)
            {
                return;
            }
            tab.Items.Remove(item);
            ctrl?.OnTabClosed(sender);
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
            }
            Current.Dispatcher.Invoke(ShowThreadException);
        }
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "エラー2", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void ShowUsage()
        {
            MessageBox.Show(Db2Source.Properties.Resources.Usage, "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            App.Current.Shutdown();
        }

        public static bool HasConnectionInfo { get; private set; } = false;
        public static string Hostname { get; private set; } = "localhost";
        public static int Port { get; private set; } = 5432;
        public static string Database { get; private set; } = "postgres";
        public static string Username { get; private set; } = "postgres";
        //public static string Password { get; private set; } = null;
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
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AnalyzeArguments(e.Args);
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
            win.UpdateLayout();
            Point p = button.PointToScreen(new Point(button.ActualWidth - win.Width, button.ActualHeight));
            win.HorizontalAlignment = HorizontalAlignment.Right;
            win.Left = p.X;
            win.Top = p.Y;
            win.Show();
            Panel pnl = win.Content as Panel;
            p = button.PointToScreen(new Point(button.ActualWidth - (win.ActualWidth + pnl.ActualWidth) / 2 - 3, button.ActualHeight));
            win.Left = p.X;
            win.Top = p.Y;
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
    }

    public class NewNpgsqlConnectionInfo: NpgsqlConnectionInfo
    {
        public const string DEFAULE_NAME = "新しい接続...";
        //public override string GetDefaultName()
        //{
        //    return "新しい接続...";
        //}
        public NewNpgsqlConnectionInfo(bool fromSetting) : base()
        {
            if (fromSetting)
            {
                ServerName = App.Hostname;
                ServerPort = App.Port;
                DatabaseName = App.Database;
                UserName = App.Username;
            }
            //Name = GetDefaultName();
            Name = DEFAULE_NAME;
        }
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
    public class ItemsSourceToColumnFilterButtonVisiblityConverter: IValueConverter
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
}