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
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(PgsqlSetting), typeof(EditPgsqlSetting), new PropertyMetadata(new PropertyChangedCallback(OnTargetPropertyChanged)));
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

        private static void OnTargetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as EditPgsqlSetting)?.OnTargetPropertyChanged(e);
        }

        public EditPgsqlSetting()
        {
            InitializeComponent();
        }

        private void buttonApplyUserSetting_Click(object sender, RoutedEventArgs e)
        {
            string[] sql = (App.CurrentDataSet as NpgsqlDataSet).GetAlterSQL(Target, App.CurrentDataSet.ConnectionInfo.UserName, string.Empty, string.Empty, 0, false);
            if (!App.ExecSqls(sql, true))
            {
                return;
            }
            DialogResult = true;
        }

        private void buttonApplySystemSetting_Click(object sender, RoutedEventArgs e)
        {
            string[] sql = (App.CurrentDataSet as NpgsqlDataSet).GetAlterSQL(Target, string.Empty, string.Empty, 0, false);
            if (!App.ExecSqls(sql, true))
            {
                return;
            }
            DialogResult = true;
        }
    }
}
