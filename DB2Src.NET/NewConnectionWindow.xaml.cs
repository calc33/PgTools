using System;
using System.Collections;
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
        public static DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(ConnectionInfo), typeof(NewConnectionWindow), new PropertyMetadata(new PropertyChangedCallback(OnTargetPropertyChanged)));
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

        private RegistryBinding _registryBinding = null;
        private void RequireRegistryBinding()
        {
            if (_registryBinding != null)
            {
                return;
            }
            Window window = Window.GetWindow(this);
            _registryBinding = new RegistryBinding(window);
            _registryBinding.Register(window, tabControlConnections);
        }
        public RegistryBinding RegistryBinding
        {
            get
            {
                RequireRegistryBinding();
                return _registryBinding;
            }
        }

        public void LoadFromRegistry()
        {
            RegistryBinding.Load(App.RegistryFinder);
        }

        public void SaveToRegistry()
        {
            RegistryBinding.Save(App.RegistryFinder);
        }
        private double _fieldMaxWidth = 0;
        private double _nameMaxWidth = 0;

        private void UpdateMaxTextWidth()
        {
            Typeface typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            DpiScale dpi = VisualTreeHelper.GetDpi(this);
			double wMax = WindowLocator.GetSizeOfCurrentScreen(this).Width / 2 - 80;
            
            _fieldMaxWidth = 0;
            _nameMaxWidth = 0;
            foreach (var info in ConnectionList)
            {

                foreach (PropertyInfo pi in info.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    InputFieldAttribute attr = (InputFieldAttribute)pi.GetCustomAttribute(typeof(InputFieldAttribute));
                    if (attr == null)
                    {
                        continue;
                    }
                    if (attr.HiddenField)
                    {
                        continue;
                    }
                    string s = pi.GetValue(info) as string;
                    if (!string.IsNullOrEmpty(s))
                    {
                        FormattedText ft = new FormattedText(s, CultureInfo.CurrentUICulture, FlowDirection, typeface, FontSize, SystemColors.ControlTextBrush, dpi.PixelsPerDip)
                        {
                            MaxTextWidth = wMax
                        };
						_fieldMaxWidth = Math.Max(_fieldMaxWidth, ft.Width);
                    }
                }
                if (!string.IsNullOrEmpty(info.Name))
                {
                    FormattedText ft = new FormattedText(info.Name, CultureInfo.CurrentUICulture, FlowDirection, typeface, FontSize, SystemColors.ControlTextBrush, dpi.PixelsPerDip)
					{
						MaxTextWidth = wMax
					};
                    _nameMaxWidth = Math.Max(_nameMaxWidth, ft.Width);
                }
            }
			gridBase.ColumnDefinitions[0].Width = new GridLength(0, GridUnitType.Auto);
			StackPanelMain.HorizontalAlignment = HorizontalAlignment.Left;
			GridProperties.ColumnDefinitions[1].Width = new GridLength(_fieldMaxWidth + 6, GridUnitType.Pixel);
			textBlockConnectionName.Width = _nameMaxWidth;
			// 個々の要素サイズを設定するとリサイズ時にもサイズ固定になるため
            // この後LayoutUpdatedイベントにてリサイズ可能なように個々の要素の設定を調整する
		}

		private TreeViewFilterKeyEventController _treeViewFilterController;

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

        private Dictionary<string, FrameworkElement> _propertyControls = new Dictionary<string, FrameworkElement>();

        private T GetPropertyControl<T>(string name) where T : FrameworkElement
        {
            if (_propertyControls.TryGetValue(name, out var control))
            {
                return control as T;
            }
            return null;
        }

        private delegate T ConstructT<T>() where T : FrameworkElement;
        private T RequirePropertyControl<T>(string name, int row, int column, ConstructT<T> ctor) where T : FrameworkElement
        {
            T control = GetPropertyControl<T>(name);
            if (control == null)
            {
                control = ctor();
                control.Name = name;
                RegisterName(name, control);
                _propertyControls[name] = control;
            }
            Grid.SetRow(control, row);
            Grid.SetColumn(control, column);
            GridProperties.Children.Add(control);
            return control;
        }

        private Type _lastTargetType = null;
        private void UpdateGridProperties()
        {
            if (Target.GetType() == _lastTargetType)
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

            foreach (FrameworkElement element in _propertyControls.Values)
            {
                GridProperties.Children.Remove(element);
            }

            GridProperties.RowDefinitions.Clear();
            GridProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            GridProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            int r = 2;
            for (int i = 0; i < props.Count; i++, r++)
            {
                GridProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                PropertyInfo prop = props[i];
                InputFieldAttribute attr = (InputFieldAttribute)prop.GetCustomAttribute(typeof(InputFieldAttribute));
                TextBlock lbl = RequirePropertyControl("textBlock" + prop.Name, r, 0, () =>
                {
                    return new TextBlock()
                    {
                        Margin = new Thickness(2.0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Text = attr?.Title
                    };
                });

                TextBox tb = RequirePropertyControl("textBox" + prop.Name, r, 1, () =>
                {
                    TextBox newObj = new TextBox()
                    {
                        Margin = new Thickness(2.0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Binding b1 = new Binding("Target." + prop.Name)
                    {
                        ElementName = "window",
                        Mode = BindingMode.TwoWay
                    };
                    newObj.SetBinding(TextBox.TextProperty, b1);
                    newObj.TextChanged += TextBox_TextChanged;
                    newObj.LostFocus += TextBox_LostFocus;
                    return newObj;
                });

                if (attr.HiddenField)
                {
                    PasswordBox pb = RequirePropertyControl("passwordBox" + prop.Name, r, 1, () =>
                    {
                        PasswordBox newObj = new PasswordBox()
                        {
                            Margin = new Thickness(2.0),
                            VerticalAlignment = VerticalAlignment.Center,
                            Password = Target.Password,
                            Tag = tb
                        };
                        newObj.PasswordChanged += PbPassword_PasswordChanged;
                        tb.TextChanged += TbPassword_TextChanged;
                        tb.IsVisibleChanged += TbPassword_IsVisibleChanged;
                        tb.Tag = newObj;
                        return newObj;
                    });

                    GridProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    r++;

                    StackPanel pnl = RequirePropertyControl<StackPanel>("stackPanel" + prop.Name, r, 1, () =>
                    {
                        StackPanel newObj = new StackPanel()
                        {
                            Orientation = Orientation.Horizontal
                        };
                        {
                            CheckBox cb = new CheckBox()
                            {
                                Content = (string)FindResource("checkBoxTextShowPassword"),
                                IsChecked = false,
                                VerticalAlignment = VerticalAlignment.Center,
                                Name = "checkBox" + prop.Name
                            };
                            RegisterName(cb.Name, cb);
                            _propertyControls[cb.Name] = cb;

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
                            newObj.Children.Add(cb);
                        }
						{
                            CheckBox cb = new CheckBox()
                            {
                                Content = (string)FindResource("checkBoxTextSavePassword"),
                                IsChecked = false,
                                VerticalAlignment = VerticalAlignment.Center,
                                Name = "checkBoxSavePassword",
                                Margin = new Thickness(4, 0, 0, 0)
							};
							Binding b1 = new Binding("Target.IsPasswordStored")
							{
								ElementName = "window",
								Mode = BindingMode.TwoWay
							};
							cb.SetBinding(CheckBox.IsCheckedProperty, b1);
							RegisterName(cb.Name, cb);
							_propertyControls[cb.Name] = cb;
							newObj.Children.Add(cb);
						}
						return newObj;
                    });
                }
            }
            GridProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            Grid.SetRow(textBlockTitleColor, r);
            Grid.SetRow(stackPanelTitleColor, r);
            r++;
            GridProperties.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            Grid.SetRow(stackPanelLastConnected, r);
            
            _lastTargetType = Target.GetType();
        }

		private void OnTargetPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue)
            {
                return;
            }
            UpdateGridProperties();
            UpdateTitleColor();
            StackPanelMain.UpdateLayout();
        }

        private static void OnTargetPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as NewConnectionWindow)?.OnTargetPropertyChanged(e);
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
            if (Target == null)
            {
                return;
            }
            if (Target.IsPasswordHidden && string.IsNullOrEmpty(textBox.Text))
            {
				Target.IsPasswordHidden = false;
			}
			CheckBox cb = FindName("checkBoxPassword") as CheckBox;
            if (cb == null)
            {
                return;
            }
            cb.IsEnabled = !Target.IsPasswordHidden;
            if (!cb.IsEnabled)
            {
                cb.IsChecked = false;
            }
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
                            if (target.Id.HasValue)
                            {
                                App.Connections.Save(target, false);
                            }
                            MessageBox.Show(App.GetExceptionMessage(t), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                            App.LogException(t);
                            gridLoading.Visibility = Visibility.Collapsed;
                        });
                    }
                    App.Connections.Save(target, true);
                });
            }
            catch (Exception t)
            {
                MessageBox.Show(App.GetExceptionMessage(t), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                App.LogException(t);
                gridLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            Target.Name = Target.GetDefaultName();
            Target = App.Connections.Merge(Target);
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
                    MessageBox.Show(App.GetExceptionMessage(t), Properties.Resources.MessageBoxCaption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                    App.LogException(t);
                    return;
                }
            }
            MessageBox.Show(Properties.Resources.MessageBoxText_Connected, Properties.Resources.MessageBoxCaption_Succeed, MessageBoxButton.OK, MessageBoxImage.Information);
        }

		private void buttonConnectionString_Click(object sender, RoutedEventArgs e)
		{
            ConnectionStringWindow dlg = new ConnectionStringWindow()
            {
                Owner = this,
                Target = Target,
            };
            CheckBox cb = FindName("checkBoxPassword") as CheckBox;
			dlg.CanShowPassword = cb?.IsChecked ?? false;
            WindowLocator.LocateNearby(sender as Button, dlg, NearbyLocation.UpLeft);
            dlg.Show();
            //string connectionString = Target.GetExampleConnectionString(hidePassword);
            //MessageBox.Show(this, connectionString, "接続文字列", MessageBoxButton.OK, MessageBoxImage.Information);

		}

		private void InitTreeViewConnections()
        {
            ConnectionInfoTreeView.AddTreeViewItem(treeViewConnections.Items, ConnectionList/*, buttonSelectTargetMenuItem_Click*/);
        }

        private static int CompareConnectionInfoByLastConnectedDesc(ConnectionInfo item1, ConnectionInfo item2)
        {
            return -DateTime.Compare(item1.LastConnected, item2.LastConnected);
        }
        private void InitListBoxRecentConnection()
        {
            List<ConnectionInfo> list = new List<ConnectionInfo>(ConnectionList.Count);
            // 直近3ヶ月以内の接続に成功した接続先のみ表示
            DateTime limit = DateTime.Today.AddMonths(-3);
            foreach (ConnectionInfo item in ConnectionList)
            {
                if (!item.IsLastConnectionFailed && limit <= item.LastConnected)
                {
                    list.Add(item);
                }
            }
            list.Sort(CompareConnectionInfoByLastConnectedDesc);
            listBoxRecentConnections.ItemsSource = list;
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
            InitListBoxRecentConnection();
            UpdateButtonShowConnectionsContentTemplate();
        }

		private void window_LayoutUpdated(object sender, EventArgs e)
		{
			// UpdateMaxTextWidth() によってサイズ設定した直後の場合、
			// リサイズ可能なように個々の要素の設定を調整する
			ColumnDefinition colDef = gridBase.ColumnDefinitions[0];
            if (colDef.Width.IsAuto)
            {
				colDef.Width = new GridLength(colDef.ActualWidth, GridUnitType.Pixel);
				textBlockConnectionName.Width = double.NaN;
				GridProperties.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                StackPanelMain.HorizontalAlignment = HorizontalAlignment.Stretch;
			}
		}

		private void window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMaxTextWidth();
			_treeViewFilterController = new TreeViewFilterKeyEventController(treeViewConnections, textBoxFilterTreeView);
            WindowLocator.AdjustMaxHeightToScreen(this);
            MinHeight = StackPanelMain.DesiredSize.Height + ActualHeight - (Content as Grid).ActualHeight;
            Height = ActualHeight;
            SizeToContent = SizeToContent.Width;
            LoadFromRegistry();
        }

        private AggregatedEventDispatcher _locationChangedDispatcher;

		private void window_LocationChanged(object sender, EventArgs e)
        {
            if (_locationChangedDispatcher == null)
            {
                _locationChangedDispatcher = new AggregatedEventDispatcher(Dispatcher, () => { WindowLocator.AdjustMaxHeightToScreen(this); }, new TimeSpan(0, 0, 3));
            }
            _locationChangedDispatcher.Touch();
		}

        private void window_Closed(object sender, EventArgs e)
        {
            SaveToRegistry();
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
                        if (!string.IsNullOrEmpty(filter))
                        {
                            node.IsExpanded = true;
                        }
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
            switch (e.Key)
            {
                case Key.Enter:
                    //FilterTreeViewConnections();
                    PerformClickAsync(buttonOK);
                    e.Handled = true;
                    break;
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
            return ShowSelectedConnection(selectedItem.Tag as ConnectionInfo);
        }

        private bool ShowSelectedConnection(ConnectionInfo item)
        {
            if (item == null)
            {
                return false;
            }
            Target = item;
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
            if (item?.Tag is ConnectionInfo)
            {
                PerformClickAsync(buttonOK);
                e.Handled = true;
            }
        }

        private void treeViewConnections_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ShowSelectedTreeViewItem(treeViewConnections.SelectedItem as TreeViewItem);
            e.Handled = true;
        }

        private void treeViewConnections_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ConnectionInfo info = null;
            TreeViewItem sel = treeViewConnections.SelectedItem as TreeViewItem;
            if (sel != null)
            {
                info = sel.Tag as ConnectionInfo;
            }
            if (info != null)
            {
                string s = (string)FindResource("deleteConnectionInfoFormat");
                treeViewConnectionsDeleteConnection.Header = string.Format(s, info.Name);
                treeViewConnectionsDeleteConnection.Tag = info;
                treeViewConnectionsDeleteConnection.IsEnabled = true;
            }
            else
            {
                string s = (string)FindResource("deleteConnectionInfo");
                treeViewConnectionsDeleteConnection.Header = s;
                treeViewConnectionsDeleteConnection.Tag = null;
                treeViewConnectionsDeleteConnection.IsEnabled = false;
            }
            //e.Handled = true;
        }

        private void treeViewConnectionsDeleteConnection_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = treeViewConnections.SelectedItem as TreeViewItem;
            if (item == null)
            {
                return;
            }
            ConnectionInfo info = item.Tag as ConnectionInfo;
            if (info == null)
            {
                return;
            }
            string caption = (string)FindResource("deleteConnectionInfoCaption");
            string msg = string.Format((string)FindResource("deleteConnectionInfoMessage"), info.Name);
            MessageBoxResult ret = MessageBox.Show(this, msg, caption, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.No);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            App.Connections.Delete(info);
            TreeViewItem parent = item.Parent as TreeViewItem;
            if (parent != null)
            {
				parent.Items.Remove(item);
			}
            else
            {
                treeViewConnections.Items.Remove(item);
			}
		}

        private void listBoxRecentConnections_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConnectionInfo info = listBoxRecentConnections.SelectedItem as ConnectionInfo;
            if (info != null)
            {
                PerformClickAsync(buttonOK);
                e.Handled = true;
            }
        }

        private void listBoxRecentConnections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowSelectedConnection(listBoxRecentConnections.SelectedItem as ConnectionInfo);
        }
    }
}
