using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// EditPgsqlSetting.xaml の相互作用ロジック
    /// </summary>
    public partial class EditPgsqlSetting : Window
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(PgsqlSetting), typeof(EditPgsqlSetting));
        public static readonly DependencyProperty SettingWidthProperty = DependencyProperty.Register("SettingWidth", typeof(double), typeof(EditPgsqlSetting));
        //public static readonly DependencyProperty EnumValuesProperty = DependencyProperty.Register("EnumValues", typeof(string[]), typeof(EditPgsqlSetting));
        public PgsqlSetting Target
        {
            get
            {
                return (PgsqlSetting)GetValue(TargetProperty);
            }
            set
            {
                if (object.Equals(Target, value))
                {
                    return;
                }
                SetValue(TargetProperty, (PgsqlSetting)value?.Clone());
            }
        }

        public double SettingWidth
        {
            get
            {
                return (double)GetValue(SettingWidthProperty);
            }
            set
            {
                SetValue(SettingWidthProperty, value);
            }
        }

        //public string[] EnumValues
        //{
        //    get
        //    {
        //        return (string[])GetValue(EnumValuesProperty);
        //    }
        //    set
        //    {
        //        SetValue(EnumValuesProperty, value);
        //    }
        //}

        public void SetTarget(PgsqlSetting value)
        {
            Target = (PgsqlSetting)value?.Clone();
        }
        private static readonly string[] BoolVals = new string[] { "off", "on" };
        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            PgsqlSetting v = (PgsqlSetting)e.NewValue;
            bool isEnum = false;
            string[] vals = null;
            switch (v.VarType)
            {
                case "bool":
                    isEnum = true;
                    vals = BoolVals;
                    break;
                case "enum":
                    isEnum = true;
                    vals = v.EnumVals;
                    break;
            }
            Binding b = new Binding("Target.Setting")
            {
                ElementName = "window"
            };
            if (isEnum)
            {
                comboBoxSetting.Visibility = Visibility.Visible;
                comboBoxSetting.SetBinding(ComboBox.SelectedItemProperty, b);
                comboBoxSetting.ItemsSource = vals;
                textBoxSetting.Visibility = Visibility.Collapsed;
                BindingOperations.ClearBinding(textBoxSetting, TextBox.TextProperty);
                Dispatcher.InvokeAsync(comboBoxSetting.Focus);
            }
            else
            {
                textBoxSetting.Visibility = Visibility.Visible;
                textBoxSetting.SetBinding(TextBox.TextProperty, b);
                comboBoxSetting.Visibility = Visibility.Collapsed;
                BindingOperations.ClearBinding(comboBoxSetting, ComboBox.SelectedItemProperty);
                Dispatcher.InvokeAsync(()=> { textBoxSetting.Focus(); textBoxSetting.SelectAll(); });
            }
        }
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                OnTargetPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public EditPgsqlSetting()
        {
            InitializeComponent();
        }

        private void buttonApplyUserSetting_Click(object sender, RoutedEventArgs e)
        {
            string sql = string.Format("alter role {0} set {1} to {2}", App.CurrentDataSet.ConnectionInfo.UserName, Target.Name, Target.Setting);
            if (!App.ExecSql(sql, true))
            {
                return;
            }
            DialogResult = true;
        }

        private void buttonApplySystemSetting_Click(object sender, RoutedEventArgs e)
        {
            string sql = string.Format("alter role all set {0} to {1}", Target.Name, Target.Setting);
            if (!App.ExecSql(sql, true))
            {
                return;
            }
            DialogResult = true;
        }
    }
}
