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
        public static readonly DependencyProperty DataSetProperty = DependencyProperty.Register("DataSet", typeof(NpgsqlDataSet), typeof(DatabaseControl));
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(PgsqlDatabase), typeof(DatabaseControl));
        public static readonly DependencyProperty UsersProperty = DependencyProperty.Register("Users", typeof(ObservableCollection<User>), typeof(DatabaseControl));
        public static readonly DependencyProperty TablespacesProperty = DependencyProperty.Register("Tablespaces", typeof(ObservableCollection<Tablespace>), typeof(DatabaseControl));
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(DatabaseControl));
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
            dataGridSetting.ItemsSource = lDest;
        }

        private void OnDataSetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            //Target = DataSet.Database as PgsqlDatabase;
            Users = new ObservableCollection<User>(DataSet.Users);
            Tablespaces = new ObservableCollection<Tablespace>(DataSet.Tablespaces);
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
            l.Add(new NameValue() { Name = string.Empty, Value = "(すべてのカテゴリ)" });
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
            DataSet = Target.Context as NpgsqlDataSet;
            UpdateComboBoxSettingCategory();
            dataGridInfo.ItemsSource = new Database[] { Target };
            UpdateDataGridSetting();
        }
        private void UpdateListBoxSelection(ListBox target)
        {
            if (target == null)
            {
                return;
            }
            if (target.SelectedItem == null && 0 < target.Items.Count)
            {
                target.SelectedItem = target.Items[0];
            }
        }
        private void OnUsersPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }
        private void OnTablespacesPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == DataSetProperty)
            {
                OnDataSetPropertyChanged(e);
            }
            if (e.Property == TargetProperty)
            {
                OnTargetPropertyChanged(e);
            }
            if (e.Property == UsersProperty)
            {
                OnUsersPropertyChanged(e);
            }
            if (e.Property == TablespacesProperty)
            {
                OnTablespacesPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public void OnTabClosing(object sender, ref bool cancel)
        {
        }

        public void OnTabClosed(object sender)
        {
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
            UpdateDataGridSetting();
        }

        private void listBoxUsers_LayoutUpdated(object sender, EventArgs e)
        {
            UpdateListBoxSelection(listBoxUsers);
        }

        private void listBoxTablespaces_LayoutUpdated(object sender, EventArgs e)
        {
            UpdateListBoxSelection(listBoxTablespaces);
        }
    }
    public class NameValue
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
    public class StrArayToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string[] strs = value as string[];
            if (strs == null || strs.Length == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(strs[0]);
            for (int i = 1; i < strs.Length; i++)
            {
                string s = strs[i];
                buf.AppendLine();
                buf.Append(s);
            }
            return buf.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
