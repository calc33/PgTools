using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    /// DatabaseControl.xaml の相互作用ロジック
    /// </summary>
    public partial class DatabaseControl : UserControl, ISchemaObjectWpfControl
    {
        public static readonly DependencyProperty DataSetProperty = DependencyProperty.Register("DataSet", typeof(NpgsqlDataSet), typeof(DatabaseControl), new PropertyMetadata(new PropertyChangedCallback(OnDataSetPropertyChanged)));
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(PgsqlDatabase), typeof(DatabaseControl), new PropertyMetadata(new PropertyChangedCallback(OnTargetPropertyChanged)));
        public static readonly DependencyProperty UsersProperty = DependencyProperty.Register("Users", typeof(ObservableCollection<User>), typeof(DatabaseControl), new PropertyMetadata(new PropertyChangedCallback(OnUsersPropertyChanged)));
        public static readonly DependencyProperty TablespacesProperty = DependencyProperty.Register("Tablespaces", typeof(ObservableCollection<Tablespace>), typeof(DatabaseControl), new PropertyMetadata(new PropertyChangedCallback(OnTablespacesPropertyChanged)));
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(DatabaseControl));
        public static readonly DependencyProperty CurrentUserProperty = DependencyProperty.Register("CurrentUser", typeof(PgsqlUser), typeof(DatabaseControl));
        public static readonly DependencyProperty ConnectionStringProperty = DependencyProperty.Register("ConnectionString", typeof(string), typeof(DatabaseControl));
		public NpgsqlDataSet DataSet
        {
            get
            {
                return (NpgsqlDataSet)GetValue(DataSetProperty);
            }
            set
            {
                SetValue(DataSetProperty, value);
            }
        }
        public PgsqlDatabase Target
        {
            get
            {
                return (PgsqlDatabase)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }
        public ObservableCollection<User> Users
        {
            get
            {
                return (ObservableCollection<User>)GetValue(UsersProperty);
            }
            set
            {
                SetValue(UsersProperty, value);
            }
        }
        public ObservableCollection<Tablespace> Tablespaces
        {
            get
            {
                return (ObservableCollection<Tablespace>)GetValue(TablespacesProperty);
            }
            set
            {
                SetValue(TablespacesProperty, value);
            }
        }

        public bool IsEditing
        {
            get
            {
                return (bool)GetValue(IsEditingProperty);
            }
            set
            {
                SetValue(IsEditingProperty, value);
            }
        }

        public PgsqlUser CurrentUser
        {
            get
            {
                return (PgsqlUser)GetValue(CurrentUserProperty);
            }
            set
            {
                SetValue(CurrentUserProperty, value);
            }
        }

        public string ConnectionString
        {
			get
			{
				return (string)GetValue(ConnectionStringProperty);
			}
			set
			{
				SetValue(ConnectionStringProperty, value);
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
                Target = value as PgsqlDatabase;
            }
        }
        public string SelectedTabKey
        {
            get
            {
                return (tabControlMain.SelectedItem as TabItem)?.Header?.ToString();
            }
            set
            {
                foreach (TabItem item in tabControlMain.Items)
                {
                    if (item.Header.ToString() == value)
                    {
                        tabControlMain.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        public string[] SettingCheckBoxNames { get { return StrUtil.EmptyStringArray; } }

        private void UpdateDataGridSetting()
        {
            PgsqlSettingCollection lSrc = Target?.Settings;
            if (lSrc == null)
            {
                dataGridSetting.ItemsSource = null;
                return;
            }
            List<PgsqlSetting> lDest = new List<PgsqlSetting>();
            string cat = comboBoxSettingCategory.SelectedValue?.ToString();
            string filter = textBoxSettingFilter.IsVisible ? textBoxSettingFilter.Text?.ToUpper() : string.Empty;
            foreach (PgsqlSetting s in lSrc)
            {
                if ((string.IsNullOrEmpty(cat) || s.Category == cat) && (string.IsNullOrEmpty(filter) || s.Name.ToUpper().Contains(filter) || s.ShortDesc.ToUpper().Contains(filter)))
                {
                    lDest.Add(s);
                }
            }
            dataGridSetting.ItemsSource = null;
            dataGridSetting.ItemsSource = lDest;
        }

        private void RefreshDataGridSetting()
        {
            Db2SourceContext dataSet = App.CurrentDataSet;
            using (System.Data.IDbConnection conn = dataSet.NewConnection(true))
            {
                dataSet.RefreshSettings(conn);
            }
            UpdateDataGridSetting();
        }

        private void OnDataSetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (DataSet == null)
            {
                return;
            }
            //Target = DataSet.Database as PgsqlDatabase;
            Users = new ObservableCollection<User>(DataSet.Users);
            Tablespaces = new ObservableCollection<Tablespace>(DataSet.Tablespaces);
            CurrentUser = DataSet.Users[DataSet.ConnectionInfo.UserName] as PgsqlUser;
        }

        private static void OnDataSetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DatabaseControl)?.OnDataSetPropertyChanged(e);
        }

        private void UpdateComboBoxSettingCategory()
        {
            List<string> strs = new List<string>();
            if (Target != null)
            {
                foreach (PgsqlSetting s in Target.Settings)
                {
                    strs.Add(s.Category);
                }
            }
            strs.Sort();
            for (int i = strs.Count - 1; 0 < i; i--)
            {
                if (strs[i] == strs[i - 1])
                {
                    strs.RemoveAt(i);
                }
            }
            string last = (string)comboBoxSettingCategory.SelectedValue;
            int idx = 0;
            List<NameValue> l = new List<NameValue>();
            l.Add(new NameValue() { Name = string.Empty, Value = (string)FindResource("categoryFilter_All") });
            foreach (string s in strs)
            {
                if (last == s)
                {
                    idx = l.Count;
                }
                l.Add(new NameValue() { Name = s, Value = s });
            }
            comboBoxSettingCategory.ItemsSource = l;
            comboBoxSettingCategory.SelectedIndex = idx;
        }
        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DataSet = Target?.Context as NpgsqlDataSet;
            UpdateComboBoxSettingCategory();
            dataGridInfo.ItemsSource = Target != null ? new Database[] { Target } : Database.EmptyArray;
            UpdateDataGridSetting();
            ConnectionString = Target?.ConnectionInfo?.GetExampleConnectionString(true);
        }

        private static void OnTargetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DatabaseControl)?.OnTargetPropertyChanged(e);
        }

        private void OnUsersPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        private static void OnUsersPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DatabaseControl)?.OnUsersPropertyChanged(e);
        }

        private void OnTablespacesPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        private static void OnTablespacesPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DatabaseControl)?.OnTablespacesPropertyChanged(e);
        }

        public void OnTabClosing(object sender, ref bool cancel)
        {
        }

        public void Dispose()
        {
            BindingOperations.ClearAllBindings(this);
        }
        public void OnTabClosed(object sender)
        {
            //Dispose();
        }

        public DatabaseControl()
        {
            InitializeComponent();
        }

        private void comboBoxSettingCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDataGridSetting();
        }

        private void textBoxSettingFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDataGridSetting();
        }

        private void toggleButtonSettingFilter_Click(object sender, RoutedEventArgs e)
        {
            UpdateDataGridSetting();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshDataGridSetting();
        }

        private void EditSettingColumnButton_Click(object sender, RoutedEventArgs e)
        {
            EditPgsqlSetting win = new EditPgsqlSetting();
            PgsqlSetting sel = dataGridSetting.CurrentItem as PgsqlSetting;
            if (sel == null)
            {
                return;
            }
            DataGridCell cell = App.FindLogicalParent<DataGridCell>(dataGridSettingValueColumn.GetCellContent(sel));
            win.SetTarget(sel);
            win.SettingWidth = dataGridSettingValueColumn.ActualWidth;
            Rect r = new Rect(-1, -2, cell.ActualWidth + 1, cell.ActualHeight + 2);
            WindowLocator.LocateNearby(cell, r, win, NearbyLocation.Overlap);
            bool? ret = win.ShowDialog();
            if (ret.HasValue && ret.Value)
            {
                RefreshDataGridSetting();
            }
        }

        private void tabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void buttonCopyConnectionString_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(textBlockConnectionString.Text);
        }
    }
}
