using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
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

        //public ConnectionInfo NewConnectionInfo { get; set; }

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

        private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
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
            GridProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            GridProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            int r = 2;
            for (int i = 0; i < props.Count; i++, r++)
            {
                GridProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                PropertyInfo prop = props[i];
                InputFieldAttribute attr = (InputFieldAttribute)prop.GetCustomAttribute(typeof(InputFieldAttribute));
                TextBlock lbl = new TextBlock()
                {
                    Margin = new Thickness(2.0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = attr?.Title
                };
                Grid.SetRow(lbl, r);
                GridProperties.Children.Add(lbl);
                string newName = "textBox" + prop.Name;
                TextBox tb = FindName(newName) as TextBox;
                if (tb == null)
                {
                    tb = new TextBox()
                    {
                        Name = newName,
                        Margin = new Thickness(2.0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Binding b1 = new Binding("Target." + prop.Name)
                    {
                        ElementName = "window",
                        Mode = BindingMode.TwoWay
                    };
                    tb.SetBinding(TextBox.TextProperty, b1);
                    tb.TextChanged += TextBox_TextChanged;
                    tb.LostFocus += TextBox_LostFocus;
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
                        pb = new PasswordBox()
                        {
                            Name = newName,
                            Margin = new Thickness(2.0),
                            VerticalAlignment = VerticalAlignment.Center,
                            Password = Target.Password,
                            Tag = tb
                        };
                        pb.PasswordChanged += PbPassword_PasswordChanged;
                        tb.TextChanged += TbPassword_TextChanged;
                        tb.IsVisibleChanged += TbPassword_IsVisibleChanged;
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

                    GridProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    r++;

                    newName = "checkBox" + prop.Name;
                    CheckBox cb = FindName(newName) as CheckBox;
                    if (cb == null)
                    {
                        cb = new CheckBox()
                        {
                            Name = newName,
                            Content = (string)Resources["checkBoxTextShowPassword"],
                            IsChecked = false,
                            VerticalAlignment = VerticalAlignment.Center
                        };
                        RegisterName(cb.Name, cb);

                        Binding b3 = new Binding("IsChecked")
                        {
                            ElementName = cb.Name,
                            Mode = BindingMode.OneWay,
                            Converter = new BooleanToVisibilityConverter()
                        };
                        tb.SetBinding(VisibilityProperty, b3);

                        Binding b4 = new Binding("IsChecked")
                        {
                            ElementName = cb.Name,
                            Mode = BindingMode.OneWay,
                            Converter = new InvertBooleanToVisibilityConverter()
                        };
                        pb.SetBinding(VisibilityProperty, b4);

                        Binding b5 = new Binding("IsEnabled")
                        {
                            RelativeSource = RelativeSource.Self,
                            Mode = BindingMode.OneWay,
                            Converter = new IsEnabledToColorConverter()
                        };
                        cb.SetBinding(ForegroundProperty, b5);
                    }
                    else
                    {
                        GridProperties.Children.Remove(cb);
                    }
                    Grid.SetColumn(cb, 1);
                    Grid.SetRow(cb, r);
                    if (Target.IsPasswordHidden)
                    {
                        cb.IsEnabled = false;
                        cb.IsChecked = false;
                    }
                    else
                    {
                        cb.IsEnabled = true;
                    }
                    GridProperties.Children.Add(cb);
                }
            }
            GridProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            Grid.SetRow(textBlockTitleColor, r);
            Grid.SetRow(stackPanelTitleColor, r);
            UpdateTitleColor();
            StackPanelMain.UpdateLayout();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateTitleColor();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTitleColor();
        }

        private void UpdateCheckBoxPasswordEnabled(TextBox textBox)
        {
            if (!Target.IsPasswordHidden)
            {
                return;
            }
            if (!string.IsNullOrEmpty(textBox.Text))
            {
                return;
            }
            Target.IsPasswordHidden = false;
            CheckBox cb = FindName("checkBoxPassword") as CheckBox;
            if (cb == null)
            {
                return;
            }
            cb.IsEnabled = !Target.IsPasswordHidden;
        }
        private void TbPassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            PasswordBox pb = (PasswordBox)tb.Tag;
            if (pb.Password != tb.Text)
            {
                pb.Password = tb.Text;
            }
            UpdateCheckBoxPasswordEnabled(tb);
        }

        private void TbPassword_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (tb == null)
            {
                return;
            }
            if (!tb.IsVisible)
            {
                return;
            }
            // UNDO履歴をクリア
            tb.IsUndoEnabled = false;
            tb.IsUndoEnabled = true;
        }

        private void PbPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox pb = (PasswordBox)sender;
            TextBox tb = (TextBox)pb.Tag;
            if (pb.Password != tb.Text)
            {
                tb.Text = pb.Password;
            }
            Target.Password = pb.Password;
            UpdateCheckBoxPasswordEnabled(tb);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == TargetProperty)
            {
                OnTargetPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }

        public NewConnectionWindow()
        {
            InitializeComponent();
        }

        private void ConnectAndCloseAsync()
        {
            gridLoading.Visibility = Visibility.Visible;
            try
            {
                Dispatcher disp = Dispatcher;
                ConnectionInfo target = Target;
                _ = Task.Run(() =>
                {
                    IDbConnection conn = null;
                    try
                    {
                        conn = target.NewConnection(false);
                        conn.Open();
                        disp.InvokeAsync(() =>
                        {
                            Result = conn;
                            gridLoading.Visibility = Visibility.Collapsed;
                            DialogResult = true;
                        });
                    }
                    catch (Exception t)
                    {
                        conn?.Dispose();
                        disp.InvokeAsync(() =>
                        {
                            MessageBox.Show(t.Message, Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                            App.LogException(t);
                            gridLoading.Visibility = Visibility.Collapsed;
                        });
                    }
                });
            }
            catch (Exception t)
            {
                MessageBox.Show(t.Message, Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                App.LogException(t);
                gridLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            Target.Name = Target.GetDefaultName();
            Target = App.Connections.Merge(Target);
            App.Connections.Save(Target);
            Dispatcher.InvokeAsync(ConnectAndCloseAsync);
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            using (IDbConnection conn = Target.NewConnection(false))
            {
                try
                {
                    conn.Open();
                }
                catch (Exception t)
                {
                    MessageBox.Show(t.Message, Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    App.LogException(t);
                    return;
                }
            }
            MessageBox.Show(Properties.Resources.MessageBoxText_Connected, Properties.Resources.MessageBoxCaption_Succeed, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void InitTreeViewConnections()
        {
            ConnectionInfoTreeView.AddTreeViewItem(treeViewConnections.Items, ConnectionList/*, buttonSelectTargetMenuItem_Click*/);
        }

        private void buttonSelectTargetMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ConnectionInfo info = (sender as MenuItem)?.Tag as ConnectionInfo;
            if (info == null)
            {
                return;
            }
            Target = info;
        }

        private void window_Initialized(object sender, EventArgs e)
        {
            List<ConnectionInfo> l = new List<ConnectionInfo>(App.Connections);
            l.Sort(ConnectionInfo.CompareByCategory);
            ConnectionInfo info0 = null;
            for (int i = 0; i < l.Count; i++)
            {
                if (l[i].ContentEquals(info0))
                {
                    info0 = l[i];
                    break;
                }
            }
            ConnectionList = l;
            Target = info0;
            UpdateTitleColor();
            InitTreeViewConnections();
            UpdateButtonShowConnectionsContentTemplate();
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowLocator.AdjustMaxHeightToScreen(this);
            MinHeight = StackPanelMain.DesiredSize.Height + ActualHeight - (Content as Grid).ActualHeight;
            Height = ActualHeight;
            SizeToContent = SizeToContent.Width;
        }

        private void window_LocationChanged(object sender, EventArgs e)
        {
            WindowLocator.AdjustMaxHeightToScreen(this);
        }

        private void UpdateTitleColor()
        {
            //buttonTitleColor.GetBindingExpression(BackgroundProperty)?.UpdateSource();
            //checkBoxTitleColor.GetBindingExpression(CheckBox.IsCheckedProperty)?.UpdateSource();
            buttonTitleColor.GetBindingExpression(BackgroundProperty)?.UpdateTarget();
            checkBoxTitleColor.GetBindingExpression(CheckBox.IsCheckedProperty)?.UpdateTarget();
        }
        private void buttonTitleColor_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow win = new ColorPickerWindow();
            WindowLocator.LocateNearby(sender as Button, win, NearbyLocation.DownLeft);
            RGB rgb = Target.BackgroundColor;
            win.Color = Color.FromRgb(rgb.R, rgb.G, rgb.B);
            win.Owner = this;
            bool? ret = win.ShowDialog();
            if (ret.HasValue && ret.Value)
            {
                rgb = new RGB(win.Color.R, win.Color.G, win.Color.B);
                Target.BackgroundColor = rgb;
                UpdateTitleColor();
            }
        }

        private void checkBoxTitleColor_Click(object sender, RoutedEventArgs e)
        {
            UpdateTitleColor();
        }

        private bool FilterTreeViewItem(ItemCollection items, string filter)
        {
            bool hasVisibleItem = false;
            foreach (object item in items)
            {
                TreeViewItem node = item as TreeViewItem;
                if (node == null)
                {
                    continue;
                }
                if (node.HasItems)
                {
                    if (FilterTreeViewItem(node.Items, filter))
                    {
                        node.Visibility = Visibility.Visible;
                        hasVisibleItem = true;
                    }
                    else
                    {
                        node.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(filter) || node.Header.ToString().Contains(filter))
                    {
                        node.Visibility = Visibility.Visible;
                        hasVisibleItem = true;
                    }
                    else
                    {
                        node.Visibility = Visibility.Collapsed;
                    }
                }
            }
            return hasVisibleItem;
        }

        private void FilterTreeViewConnections()
        {
            string s = textBoxFilterTreeView.Text;
            FilterTreeViewItem(treeViewConnections.Items, s);
        }

        private void textBoxFilterTreeView_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTreeViewConnections();
        }

        private void textBoxFilterTreeView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FilterTreeViewConnections();
                e.Handled = true;
            }
        }

        private void UpdateButtonShowConnectionsContentTemplate()
        {
            bool isChecked = buttonShowConnections.IsChecked ?? false;
            string resName = isChecked ? "ImageLeftArrow8": "ImageRightArrow8";
            buttonShowConnections.ContentTemplate = buttonShowConnections.FindResource(resName) as DataTemplate;
        }

        private void buttonShowConnections_Click(object sender, RoutedEventArgs e)
        {
            UpdateButtonShowConnectionsContentTemplate();
        }

        private bool ShowSelectedTreeViewItem(TreeViewItem selectedItem)
        {
            if (selectedItem == null)
            {
                return false;
            }
            ConnectionInfo info = selectedItem.Tag as ConnectionInfo;
            if (info == null)
            {
                return false;
            }
            Target = info;
            return true;
        }

        private void PerformClickAsync(Button button)
        {
            Dispatcher.InvokeAsync(() => {
                var provider = new ButtonAutomationPeer(button) as IInvokeProvider;
                provider.Invoke();
            });
        }

        private void treeViewConnections_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem item = treeViewConnections.SelectedItem as TreeViewItem;
            if (item?.Tag != null)
            {
                PerformClickAsync(buttonOK);
                e.Handled = true;
            }
        }

        private void treeViewConnections_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TreeViewItem item = treeViewConnections.SelectedItem as TreeViewItem;
                if (item?.Tag != null)
                {
                    PerformClickAsync(buttonOK);
                    e.Handled = true;
                }
            }
        }

        private void treeViewConnections_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ShowSelectedTreeViewItem(treeViewConnections.SelectedItem as TreeViewItem);
            e.Handled = true;
        }
    }
}
