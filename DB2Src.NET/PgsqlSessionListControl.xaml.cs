using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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

namespace Db2Source
{
    /// <summary>
    /// PgsqlSessionListControl.xaml の相互作用ロジック
    /// </summary>
    public partial class PgsqlSessionListControl : UserControl, ISchemaObjectWpfControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(SessionList), typeof(PgsqlSessionListControl));

        public SessionList Target
        {
            get
            {
                return (SessionList)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

        SchemaObject ISchemaObjectControl.Target
        {
            get
            {
                return Target;
            }
            set
            {
                Target = value as SessionList;
            }
        }
        public string SelectedTabKey { get { return string.Empty; } set { } }

        public string[] SettingCheckBoxNames { get { return StrUtil.EmptyStringArray; } }

        public List<ISession> AllSessions { get; private set; }

        public PgsqlSessionListControl()
        {
            InitializeComponent();
        }

        private static bool Matches(string value, ComboBox comboBox)
        {
            if (string.IsNullOrEmpty(comboBox.Text))
            {
                return true;
            }
            if (value == comboBox.Text)
            {
                return true;
            }
            return false;
        }
        private static void AddList(List<string> list, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            list.Add(value);
        }
        private static void Compact(List<string> list)
        {
            list.Sort();
            for (int i = list.Count - 1; 0 < i; i--)
            {
                if (list[i] == list[i - 1])
                {
                    list.RemoveAt(i);
                }
            }
            list.Insert(0, string.Empty);
        }
        private void UpdateFilterComboBox()
        {
            List<string> lDb = new List<string>();
            List<string> lUser = new List<string>();
            List<string> lApp = new List<string>();
            List<string> lHost = new List<string>();
            foreach (NpgsqlSession s in AllSessions)
            {
                AddList(lDb, s.DatabaseName);
                AddList(lUser, s.UserName);
                AddList(lApp, s.ApplicationName);
                AddList(lHost, s.Client);
                AddList(lHost, s.Address);
            }
            Compact(lDb);
            Compact(lUser);
            Compact(lApp);
            Compact(lHost);
            comboBoxFilterDatabase.ItemsSource = lDb;
            comboBoxFilterUser.ItemsSource = lUser;
            comboBoxFilterApplication.ItemsSource = lApp;
            comboBoxFilterHost.ItemsSource = lHost;
        }
        private void UpdateSessions()
        {
            List<NpgsqlSession> l = new List<NpgsqlSession>();
            foreach (NpgsqlSession s in AllSessions)
            {
                if (Matches(s.DatabaseName, comboBoxFilterDatabase)
                    && Matches(s.UserName, comboBoxFilterUser)
                    && Matches(s.ApplicationName, comboBoxFilterApplication)
                    && (Matches(s.Client, comboBoxFilterHost) || Matches(s.Address, comboBoxFilterHost)))
                {
                    l.Add(s);
                }
            }
            dataGridSessionList.ItemsSource = l;
        }
        public void Refresh()
        {
            AllSessions = Target?.GetSessions();
            UpdateFilterComboBox();
            UpdateSessions();
        }

        public void OnTabClosing(object sender, ref bool cancel) { }

        public void Dispose()
        {
            BindingOperations.ClearAllBindings(this);
        }

        public void OnTabClosed(object sender)
        {
            Dispose();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void menuItemKillSession_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                return;
            }
            NpgsqlSession obj = dataGridSessionList.SelectedItem as NpgsqlSession;
            if (obj == null)
            {
                return;
            }
            string msg = string.Format((string)Resources["messageDisconnectForce"],
                obj.Hostname, obj.ApplicationName, obj.UserName, obj.DatabaseName, obj.Pid);
            MessageBoxResult ret = MessageBox.Show(msg, (string)Resources["captionDisconnectForce"], MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            using (IDbConnection conn = Target.Context.NewConnection(true))
            {
                obj.Kill(conn);
            }
        }

        private void menuItemAbortQuery_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                return;
            }
            NpgsqlSession obj = dataGridSessionList.SelectedItem as NpgsqlSession;
            if (obj == null)
            {
                return;
            }
            string msg = string.Format((string)Resources["messageAbortQuery"],
                obj.Hostname, obj.ApplicationName, obj.UserName, obj.DatabaseName, obj.Pid);
            MessageBoxResult ret = MessageBox.Show(msg, (string)Resources["captionAbortQuery"], MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            using (IDbConnection conn = Target.Context.NewConnection(true))
            {
                obj.AbortQuery(conn);
            }
        }

        private void comboBoxFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSessions();
        }

        private void comboBoxFilter_DropDownClosed(object sender, EventArgs e)
        {
            UpdateSessions();
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = dataGridSessionList.ContextMenu;
            menu.Placement = PlacementMode.Bottom;
            menu.PlacementTarget = buttonStop;
            menu.IsOpen = true;
        }
    }
}
