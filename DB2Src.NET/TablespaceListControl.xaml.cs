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
    /// TablespaceListControl.xaml の相互作用ロジック
    /// </summary>
    public partial class TablespaceListControl : UserControl
    {
        public static readonly DependencyProperty TablespacesProperty = DependencyProperty.Register("Tablespaces", typeof(ObservableCollection<Tablespace>), typeof(TablespaceListControl));
        public static readonly DependencyProperty CurrentProperty = DependencyProperty.Register("Current", typeof(PgsqlTablespace), typeof(TablespaceListControl), new PropertyMetadata(new PropertyChangedCallback(OnCurrentPropertyChanged)));
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(PgsqlTablespace), typeof(TablespaceListControl), new PropertyMetadata(new PropertyChangedCallback(OnTargetPropertyChanged)));
        //public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register("IsModified", typeof(bool), typeof(TableSpaceListControl));
        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(TablespaceListControl));

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

        public PgsqlTablespace Current
        {
            get
            {
                return (PgsqlTablespace)GetValue(CurrentProperty);
            }
            set
            {
                SetValue(CurrentProperty, value);
            }
        }

        public PgsqlTablespace Target
        {
            get
            {
                return (PgsqlTablespace)GetValue(TargetProperty);
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
            return Current != null && Current.Oid == 0;
        }

        private void RemoveCurrent()
        {
            int i = listBoxTablespaces.SelectedIndex;
            Tablespaces.Remove(Current);
            i = Math.Min(Math.Max(0, i), App.CurrentDataSet.Tablespaces.Count - 1);
            listBoxTablespaces.SelectedIndex = i;
        }

        private void Revert()
        {
            if (Current != null)
            {
                Target = new PgsqlTablespace(null, Current);
            }
            else
            {
                Target = null;
            }
        }

        private void UpdateTablespaces()
        {
            NamedCollection<Tablespace> l = App.CurrentDataSet?.Tablespaces;
            if (l != null)
            {
                Tablespaces = new ObservableCollection<Tablespace>(l);
            }
            else
            {
                Tablespaces = new ObservableCollection<Tablespace>();
            }
        }
        private void RefreshTablespaces()
        {
            int i = listBoxTablespaces.SelectedIndex;
            App.CurrentDataSet.RefreshTablespaces();
            i = Math.Min(Math.Max(0, i), App.CurrentDataSet.Tablespaces.Count - 1);
            listBoxTablespaces.SelectedIndex = i;
        }

        private void OnCurrentPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            Revert();
        }

        private static void OnCurrentPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as TablespaceListControl)?.OnCurrentPropertyChanged(e);
        }

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        private static void OnTargetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as TablespaceListControl)?.OnTargetPropertyChanged(e);
        }

        public TablespaceListControl()
        {
            InitializeComponent();
        }

        private void buttonDropTablespace_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                return;
            }
            Window owner = Window.GetWindow(this);
            MessageBoxResult ret = MessageBox.Show(owner, string.Format((string)FindResource("messageDropTablespace"), Target.Name), Properties.Resources.MessageBoxCaption_Drop, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            if (IsNew())
            {
                RemoveCurrent();
                return;
            }
            string[] sqls = App.CurrentDataSet.GetDropSQL(Target, true, string.Empty, string.Empty, 0, false, false);
            if (App.ExecSqls(sqls, false, this))
            {
                RefreshTablespaces();
            }
        }

        private void listBoxTablespaces_LayoutUpdated(object sender, EventArgs e)
        {
            if (listBoxTablespaces.SelectedItem == null && 0 < listBoxTablespaces.Items.Count)
            {
                listBoxTablespaces.SelectedItem = listBoxTablespaces.Items[0];
            }
        }

        private void buttonAddTablespace_Click(object sender, RoutedEventArgs e)
        {
            PgsqlTablespace newItem = new PgsqlTablespace(null);
            Tablespaces.Add(newItem);
            Dispatcher.InvokeAsync(() =>
            {
                listBoxTablespaces.SelectedItem = newItem;
                IsEditing = true;
            }, DispatcherPriority.ApplicationIdle);
        }

        private void buttonApply_Click(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                IsEditing = false;
                return;
            }
            if (!Target.ContentEquals(Current))
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
            if (App.ExecSqls(sqls, false, this))
            {
                IsEditing = false;
                RefreshTablespaces();
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
            UpdateTablespaces();
        }
    }
}
