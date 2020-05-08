using System;
using System.Collections.Generic;
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
            tab.Items.Remove(item);
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
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AnalyzeArguments(e.Args);
        }
    }

    public class NewNpgsqlConnectionInfo: NpgsqlConnectionInfo
    {
        public override string GetDefaultName()
        {
            return "新しい接続...";
        }
        public NewNpgsqlConnectionInfo() : base()
        {
            ServerName = App.Hostname;
            ServerPort = App.Port;
            DatabaseName = App.Database;
            UserName = App.Username;
            Name = GetDefaultName();
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
}