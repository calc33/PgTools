﻿using System;
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
        public static DependencyProperty ConnectionListProperty = DependencyProperty.Register("ConnectionList", typeof(ConnectionList), typeof(NewConnectionWindow));
        public static DependencyProperty TargetProperty = DependencyProperty.Register("Target", typeof(ConnectionInfo), typeof(NewConnectionWindow));
        public ConnectionList ConnectionList
        {
            get
            {
                return (ConnectionList)GetValue(ConnectionListProperty);
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
                            Content = "パスワードを表示",
                            IsChecked = false
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

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Target.Name = Target.GetDefaultName();
                IDbConnection conn = Target.NewConnection(true);
                Result = conn;
            }
            catch (Exception t)
            {
                MessageBox.Show(t.Message, "接続エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ConnectionList.Save(Target);
            DialogResult = true;
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

        private void window_Initialized(object sender, EventArgs e)
        {
            ConnectionList l = App.Connections;
            ConnectionInfo info0 = new NewNpgsqlConnectionInfo(true);
            info0.FillStoredPassword(false);
            l.Insert(0, info0);
            for (int i = 1; i < l.Count; i++)
            {
                if (l[i].ContentEquals(info0))
                {
                    info0 = l[i];
                    l[0] = new NewNpgsqlConnectionInfo(false);
                    break;
                }
            }
            ConnectionList = l;
            Target = info0;
            UpdateTitleColor();
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
    }
}
