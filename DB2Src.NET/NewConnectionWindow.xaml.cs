using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
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

namespace Db2Source
{
    /// <summary>
    /// NewConnectionWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NewConnectionWindow: Window
    {
        public static DependencyProperty ConnectionListProperty = DependencyProperty.Register("ConnectionList", typeof(List<ConnectionInfo>), typeof(NewConnectionWindow));
        public static DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(ConnectionInfo), typeof(NewConnectionWindow));
        public List<ConnectionInfo> ConnectionList
        {
            get
            {
                return (List<ConnectionInfo>)GetValue(ConnectionListProperty);
            }
            set
            {
                SetValue(ConnectionListProperty, value);
            }
        }
        public ConnectionInfo Target
        {
            get
            {
                return (ConnectionInfo)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

        public IDbConnection Result { get; private set; }

        private static int ComparePropertyByInputFieldAttr(PropertyInfo item1, PropertyInfo item2)
        {
            if (item1 == null || item2 == null)
            {
                return (item1 == null ? 1 : 0) - (item2 == null ? 1 : 0);
            }
            InputFieldAttribute attr1 = (InputFieldAttribute)item1.GetCustomAttribute(typeof(InputFieldAttribute));
            InputFieldAttribute attr2 = (InputFieldAttribute)item2.GetCustomAttribute(typeof(InputFieldAttribute));
            if (attr1 == null || attr2 == null)
            {
                return (attr1 == null ? 1 : 0) - (attr2 == null ? 1 : 0);
            }
            return attr1.Order.CompareTo(attr2.Order);
        }
        private void OnTargetChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue)
            {
                return;
            }
            List<PropertyInfo> props = new List<PropertyInfo>();
            foreach (PropertyInfo pi in Target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                InputFieldAttribute attr = (InputFieldAttribute)pi.GetCustomAttribute(typeof(InputFieldAttribute));
                if (attr != null)
                {
                    props.Add(pi);
                }
            }
            props.Sort(ComparePropertyByInputFieldAttr);

            GridProperties.RowDefinitions.Clear();
            RowDefinition row = new RowDefinition();
            row.Height = GridLength.Auto;
            GridProperties.RowDefinitions.Add(row);
            row = new RowDefinition();
            row.Height = GridLength.Auto;
            GridProperties.RowDefinitions.Add(row);
            int r = 2;
            for (int i = 0; i < props.Count; i++, r++)
            {
                row = new RowDefinition();
                row.Height = GridLength.Auto;
                GridProperties.RowDefinitions.Add(row);
                PropertyInfo prop = props[i];
                InputFieldAttribute attr = (InputFieldAttribute)prop.GetCustomAttribute(typeof(InputFieldAttribute));
                TextBlock lbl = new TextBlock();
                lbl.Margin = new Thickness(2.0);
                lbl.HorizontalAlignment = HorizontalAlignment.Right;
                lbl.Text = attr?.Title;
                Grid.SetRow(lbl, r);
                GridProperties.Children.Add(lbl);
                string newName = "textBox" + prop.Name;
                TextBox tb = FindName(newName) as TextBox;
                if (tb == null) {
                    tb = new TextBox();
                    tb.Name = newName;
                    tb.Margin = new Thickness(2.0);
                    Binding b1 = new Binding("Target." + prop.Name);
                    b1.ElementName = "window";
                    b1.Mode = BindingMode.TwoWay;
                    tb.SetBinding(TextBox.TextProperty, b1);
                    RegisterName(tb.Name, tb);
                }
                else
                {
                    GridProperties.Children.Remove(tb);
                }
                Grid.SetColumn(tb, 1);
                Grid.SetRow(tb, r);
                GridProperties.Children.Add(tb);
                if (attr.HiddenField)
                {
                    newName = "passwordBox" + prop.Name;
                    PasswordBox pb = FindName(newName) as PasswordBox;
                    if (pb == null)
                    {
                        pb = new PasswordBox();
                        pb.Name = newName;
                        pb.Margin = new Thickness(2.0);
                        //Binding b2 = new Binding("Text");
                        //b2.ElementName = tb.Name;
                        //b2.Mode = BindingMode.TwoWay;
                        //pb.SetBinding(PasswordBox.TextProperty, b2);
                        pb.Password = Target.Password;
                        pb.PasswordChanged += PbPassword_PasswordChanged;
                        pb.Tag = tb;
                        tb.TextChanged += TbPassword_TextChanged;
                        tb.Tag = pb;
                        RegisterName(pb.Name, pb);
                    }
                    else
                    {
                        GridProperties.Children.Remove(pb);
                    }
                    Grid.SetColumn(pb, 1);
                    Grid.SetRow(pb, r);
                    GridProperties.Children.Add(pb);

                    row = new RowDefinition();
                    row.Height = GridLength.Auto;
                    GridProperties.RowDefinitions.Add(row);
                    r++;

                    newName = "checkBox" + prop.Name;
                    CheckBox cb = FindName(newName) as CheckBox;
                    if (cb == null)
                    {
                        cb = new CheckBox();
                        cb.Name = newName;
                        cb.Content = "パスワードを表示";
                        cb.IsChecked = false;
                        RegisterName(cb.Name, cb);

                        Binding b3 = new Binding("IsChecked");
                        b3.ElementName = cb.Name;
                        b3.Mode = BindingMode.OneWay;
                        b3.Converter = new BooleanToVisibilityConverter();
                        tb.SetBinding(VisibilityProperty, b3);

                        Binding b4 = new Binding("IsChecked");
                        b4.ElementName = cb.Name;
                        b4.Mode = BindingMode.OneWay;
                        b4.Converter = new InvertBooleanToVisibilityConverter();
                        pb.SetBinding(VisibilityProperty, b4);
                    }
                    else
                    {
                        GridProperties.Children.Remove(cb);
                    }
                    Grid.SetColumn(cb, 1);
                    Grid.SetRow(cb, r);
                    GridProperties.Children.Add(cb);
                }
            }
            StackPanelMain.UpdateLayout();
        }

        private void TbPassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            PasswordBox pb = (PasswordBox)tb.Tag;
            if (pb.Password != tb.Text)
            {
                pb.Password = tb.Text;
            }
        }

        private void PbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox pb = (PasswordBox)sender;
            TextBox tb = (TextBox)pb.Tag;
            if (pb.Password != tb.Text)
            {
                tb.Text = pb.Password;
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                OnTargetChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public NewConnectionWindow()
        {
            InitializeComponent();
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Target.Name = Target.GetDefaultName();
                IDbConnection conn = Target.NewConnection();
                Result = conn;
            }
            catch (Exception t)
            {
                MessageBox.Show(t.Message, "接続エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            DialogResult = true;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            using (IDbConnection conn = Target.NewConnection())
            {
                try
                {
                    conn.Open();
                }
                catch (Exception t)
                {
                    MessageBox.Show(t.Message, "接続エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            MessageBox.Show("接続成功", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void comboBoxConnections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            List<ConnectionInfo> l = new List<ConnectionInfo>(NpgsqlConnectionInfo.GetKnownConnectionInfos());
            ConnectionInfo info0 = new NewNpgsqlConnectionInfo();
            l.Insert(0, info0);
            ConnectionList = l;
            Target = info0;
        }
    }
    public class InvertBooleanToVisibilityConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return Visibility.Hidden;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((Visibility)value)
            {
                case Visibility.Visible:
                    return false;
                case Visibility.Hidden:
                case Visibility.Collapsed:
                    return true;
            }
            return false;
        }
    }
}
