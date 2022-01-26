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
        public static readonly DependencyProperty GrantTextProperty = DependencyProperty.Register("GrantText", typeof(string), typeof(PgsqlUserControl));
        //public static readonly DependencyProperty CanEditPasswordProperty = DependencyProperty.Register("CanEditPassword", typeof(bool), typeof(PgsqlUserControl));
        //public static readonly DependencyProperty CanVerifyPasswordProperty = DependencyProperty.Register("CanVerifyPassword", typeof(bool), typeof(PgsqlUserControl));

        public PgsqlUser Target
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

        public string GrantText
        {
            get { return (string)GetValue(GrantTextProperty); }
            set { SetValue(GrantTextProperty, value); }
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

        private bool _isUpdateGrantTextPosted = false;
        private void UpdateGrantText()
        {
            _isUpdateGrantTextPosted = false;
            List<string> l = new List<string>();
            foreach (UIElement element in wrapPanelGrant.Children)
            {
                if (!(element is CheckBox))
                {
                    continue;
                }
                CheckBox cb = (CheckBox)element;
                if (cb.IsChecked.HasValue && cb.IsChecked.Value)
                {
                    l.Add(cb.Content.ToString());
                }
            }
            if (l.Count == 0)
            {
                GrantText = string.Empty;
                return;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(l[0]);
            for (int i = 1; i < l.Count; i++)
            {
                buf.Append(", ");
                buf.Append(l[i]);
            }
            GrantText = buf.ToString();
        }

        private void DelayedUpdateGrantText()
        {
            if (_isUpdateGrantTextPosted)
            {
                return;
            }
            _isUpdateGrantTextPosted = true;
            Dispatcher.InvokeAsync(UpdateGrantText);
        }

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
            {
                return;
            }
            if (e.OldValue != null)
            {
                ((PgsqlUser)e.OldValue).PropertyChanged -= Target_PropertyChanged;
            }
            if (e.NewValue != null)
            {
                ((PgsqlUser)e.NewValue).PropertyChanged += Target_PropertyChanged;
            }
            DelayedUpdateGrantText();
        }

        private void Target_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            DelayedUpdateGrantText();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                OnTargetPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public PgsqlUserControl()
        {
            InitializeComponent();
        }
    }
}
