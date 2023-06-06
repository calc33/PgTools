using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Db2Source
{
    /// <summary>
    /// ChangePasswordWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ChangePasswordWindow : Window
    {
        public static readonly DependencyProperty Password1Property = DependencyProperty.Register("Password1", typeof(string), typeof(ChangePasswordWindow), new PropertyMetadata(new PropertyChangedCallback(OnPassword1PropertyChanged)));
        public static readonly DependencyProperty Password2Property = DependencyProperty.Register("Password2", typeof(string), typeof(ChangePasswordWindow), new PropertyMetadata(new PropertyChangedCallback(OnPassword2PropertyChanged)));
        public static readonly DependencyProperty IsPasswordMatchedProperty = DependencyProperty.Register("IsPasswordMatched", typeof(bool), typeof(ChangePasswordWindow));

        public string Password1
        {
            get { return (string)GetValue(Password1Property); }
            set { SetValue(Password1Property, value); }
        }

        public string Password2
        {
            get { return (string)GetValue(Password2Property); }
            set { SetValue(Password2Property, value); }
        }

        public bool IsPasswordMatched
        {
            get { return (bool)GetValue(IsPasswordMatchedProperty); }
        }

        public event EventHandler<QueryResultEventArgs> Executed;

        private void UpdateIsPasswordMatched()
        {
            bool value = (buttonHiddenPassword.IsChecked ?? false) || Password1 == Password2;
            SetValue(IsPasswordMatchedProperty, value);
        }

        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        private void OnPassword1PropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (passwordBox1.Password != Password1)
            {
                passwordBox1.Password = Password1;
            }
            UpdateIsPasswordMatched();
        }

        private static void OnPassword1PropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as ChangePasswordWindow)?.OnPassword1PropertyChanged(e);
        }

        private void OnPassword2PropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (passwordBox2.Password != Password2)
            {
                passwordBox2.Password = Password2;
            }
            UpdateIsPasswordMatched();
        }

        private static void OnPassword2PropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as ChangePasswordWindow)?.OnPassword2PropertyChanged(e);
        }

        private void passwordBox1_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (Password1 != passwordBox1.Password)
            {
                Password1 = passwordBox1.Password;
            }
        }

        private void passwordBox2_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (Password2 != passwordBox2.Password)
            {
                Password2 = passwordBox2.Password;
            }
        }

        private void passwordBox2_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateIsPasswordMatched();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            UpdateIsPasswordMatched();
            if (!IsPasswordMatched)
            {
                MessageBox.Show(this, (string)Resources["messageUnmatchedPasswords"], Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            QueryResultEventArgs arg = new QueryResultEventArgs();
            Executed?.Invoke(this, arg);
            if (arg.IsFailed)
            {
                return;
            }
            DialogResult = true;
        }
    }
}
