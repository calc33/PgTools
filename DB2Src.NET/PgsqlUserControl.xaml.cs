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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Db2Source
{
    /// <summary>
    /// PgsqlUserControl.xaml の相互作用ロジック
    /// </summary>
    public partial class PgsqlUserControl : UserControl
    {
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(PgsqlUser), typeof(PgsqlUserControl));
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(PgsqlUserControl));
        public static readonly DependencyProperty IsNewProperty = DependencyProperty.Register("IsNew", typeof(bool), typeof(PgsqlUserControl));
        //public static readonly DependencyProperty CanEditPasswordProperty = DependencyProperty.Register("CanEditPassword", typeof(bool), typeof(PgsqlUserControl));
        //public static readonly DependencyProperty CanVerifyPasswordProperty = DependencyProperty.Register("CanVerifyPassword", typeof(bool), typeof(PgsqlUserControl));

        public  PgsqlUser Target
        {
            get { return (PgsqlUser)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public bool IsNew
        {
            get { return (bool)GetValue(IsNewProperty); }
            set { SetValue(IsNewProperty, value); }
        }

        //public bool CanEditPassword
        //{
        //    get { return (bool)GetValue(CanEditPasswordProperty); }
        //    set { SetValue(CanEditPasswordProperty, value); }
        //}

        //public bool CanVerifyPassword
        //{
        //    get { return (bool)GetValue(CanVerifyPasswordProperty); }
        //    set { SetValue(CanVerifyPasswordProperty, value); }
        //}

        //protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        //{
        //    base.OnPropertyChanged(e);
        //}

        public PgsqlUserControl()
        {
            InitializeComponent();
        }
    }
}
