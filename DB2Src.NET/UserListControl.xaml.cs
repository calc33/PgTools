﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// UserListControl.xaml の相互作用ロジック
    /// </summary>
    public partial class UserListControl : UserControl
    {
        public static readonly DependencyProperty UsersProperty = DependencyProperty.Register("Users", typeof(ObservableCollection<User>), typeof(UserListControl));
        public static readonly DependencyProperty CurrentProperty = DependencyProperty.Register("Current", typeof(PgsqlUser), typeof(UserListControl));
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(PgsqlUser), typeof(UserListControl));
        //public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register("IsModified", typeof(bool), typeof(UserListControl));
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(UserListControl));

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

        public PgsqlUser Current
        {
            get
            {
                return (PgsqlUser)GetValue(CurrentProperty);
            }
            set
            {
                SetValue(CurrentProperty, value);
            }
        }

        public PgsqlUser Target
        {
            get
            {
                return (PgsqlUser)GetValue(TargetProperty);
            }
            set
            {
                SetValue(TargetProperty, value);
            }
        }

        //public bool IsModified
        //{
        //    get
        //    {
        //        return (bool)GetValue(IsModifiedProperty);
        //    }
        //    set
        //    {
        //        SetValue(IsModifiedProperty, value);
        //    }
        //}

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

        public bool IsNew()
        {
            return Current.Oid == 0;
        }

        private void RemoveCurrent()
        {
            int i = listBoxUsers.SelectedIndex;
            Users.Remove(Current);
            i = Math.Min(Math.Max(0, i), App.CurrentDataSet.Users.Count - 1);
            listBoxUsers.SelectedIndex = i;
        }

        private void Revert()
        {
            if (Current != null)
            {
                Target = new PgsqlUser(null, Current);
            }
            else
            {
                Target = null;
            }
        }

        private void UpdateUsers()
        {
            NamedCollection<User> l = App.CurrentDataSet?.Users;
            if (l != null)
            {
                Users = new ObservableCollection<User>(l);
            }
            else
            {
                Users = new ObservableCollection<User>();
            }
        }

        private void RefreshUsers()
        {
            int i = listBoxUsers.SelectedIndex;
            App.CurrentDataSet.RefreshUsers();
            UpdateUsers();
            i = Math.Min(Math.Max(0, i), App.CurrentDataSet.Users.Count - 1);
            listBoxUsers.SelectedIndex = i;
        }

        private void OnCurrentPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            Revert();
        }

        //private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        //{
        //}

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            //if (e.Property == TargetProperty)
            //{
            //    OnTargetPropertyChanged(e);
            //}
            if (e.Property == CurrentProperty)
            {
                OnCurrentPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public UserListControl()
        {
            InitializeComponent();
        }

        private void buttonDropUser_Click(object sender, RoutedEventArgs e)
        {
            if (Current == null)
            {
                return;
            }
            Window owner = Window.GetWindow(this);
            MessageBoxResult ret = MessageBox.Show(owner, string.Format((string)Resources["messageDropUser"], Current.Name), Properties.Resources.MessageBoxCaption_Drop, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            if (IsNew())
            {
                RemoveCurrent();
                return;
            }

            string[] sqls = App.CurrentDataSet.GetDropSQL(Current, string.Empty, string.Empty, 0, false, false);
            if (App.ExecSqls(sqls))
            {
                RemoveCurrent();
                RefreshUsers();
            }
        }

        private void listBoxUsers_LayoutUpdated(object sender, EventArgs e)
        {
            if (listBoxUsers.SelectedItem == null && 0 < listBoxUsers.Items.Count)
            {
                listBoxUsers.SelectedItem = listBoxUsers.Items[0];
            }
        }

        private void buttonAddUser_Click(object sender, RoutedEventArgs e)
        {
            PgsqlUser newItem = new PgsqlUser(null);
            Users.Add(newItem);
            Dispatcher.InvokeAsync(() => { listBoxUsers.SelectedItem = newItem; }, DispatcherPriority.ApplicationIdle);
        }

        private void buttonApply_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                IsEditing = false;
                return;
            }
            if (Current != null && Target.ContentEquals(Current))
            {
                IsEditing = false;
                return;
            }
            string[] sqls;
            if (IsNew())
            {
                sqls = App.CurrentDataSet.GetSQL(Target, string.Empty, string.Empty, 0, false);
            }
            else
            {
                sqls = App.CurrentDataSet.GetAlterSQL(Target, Current, string.Empty, string.Empty, 0, false);
            }
            if (App.ExecSqls(sqls))
            {
                IsEditing = false;
                RefreshUsers();
            }
        }

        private void buttonRevert_Click(object sender, RoutedEventArgs e)
        {
            if (IsNew())
            {
                RemoveCurrent();
            }
            else
            {
                Revert();
            }
            IsEditing = false;
        }

        private void userControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUsers();
        }
    }
}
