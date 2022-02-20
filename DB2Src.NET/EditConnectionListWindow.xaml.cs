using System;
using System.Collections.Generic;
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
using System.Windows.Threading;

namespace Db2Source
{
    /// <summary>
    /// EditConnectionListWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class EditConnectionListWindow: Window
    {
        public class ColumnInfo: IComparable
        {
            public string PropertyName { get; set; }
            public int Order { get; set; }
            public string HeaderText { get; set; }

            internal ColumnInfo(PropertyInfo info, InputFieldAttribute attribute)
            {
                PropertyName = info.Name;
                Order = attribute.Order;
                HeaderText = attribute.Title;
            }

            public static ColumnInfo[] GetColumnInfos(IEnumerable<Type> types)
            {
                List<ColumnInfo> l = new List<ColumnInfo>();
                Dictionary<string, ColumnInfo> dict = new Dictionary<string, ColumnInfo>();
                foreach (Type t in types)
                {
                    foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        InputFieldAttribute attr = prop.GetCustomAttribute(typeof(InputFieldAttribute)) as InputFieldAttribute;
                        if (attr == null)
                        {
                            continue;
                        }
                        if (attr.HiddenField)
                        {
                            // パスワード項目は除外
                            continue;
                        }
                        ColumnInfo info = new ColumnInfo(prop, attr);
                        if (dict.ContainsKey(info.PropertyName))
                        {
                            continue;
                        }
                        l.Add(info);
                        dict.Add(info.PropertyName, info);
                    }
                }
                l.Sort();
                return l.ToArray();
            }

            public int CompareTo(object obj)
            {
                ColumnInfo ci = obj as ColumnInfo;
                if (ci == null)
                {
                    return -1;
                }
                int ret = Order.CompareTo(ci.Order);
                if (ret != 0)
                {
                    return ret;
                }
                return PropertyName.CompareTo(ci.PropertyName);
            }

            public override bool Equals(object obj)
            {
                ColumnInfo ci = obj as ColumnInfo;
                if (ci == null)
                {
                    return false;
                }
                return PropertyName == ci.PropertyName;
            }

            public override int GetHashCode()
            {
                return PropertyName.GetHashCode();
            }

            public override string ToString()
            {
                return PropertyName;
            }
        }
        public EditConnectionListWindow()
        {
            InitializeComponent();
        }
        
        private void InitDataGridConnections()
        {
            ColumnInfo[] infos = ColumnInfo.GetColumnInfos(ConnectionList.ConnectionInfoTypes);
            dataGridConnections.Columns.Clear();
            dataGridConnections.Columns.Add(new DataGridTextColumn()
            {
                Header = (string)Resources["columnHeaderDatabaseType"],
                Binding = new Binding("DatabaseType")
            });
            foreach (ColumnInfo info in infos)
            {
                dataGridConnections.Columns.Add(new DataGridTextColumn()
                {
                    Header = info.HeaderText,
                    Binding = new Binding(info.PropertyName)
                });
            }
            dataGridConnections.ItemsSource = App.Connections;
        }

        //private void InitButtonAddContextMenu()
        //{
        //    ContextMenu menu = new ContextMenu();
        //    ItemCollection l = menu.Items;
        //    l.Clear();
        //    foreach (Type t in ConnectionList.ConnectionInfoTypes)
        //    {
        //        ConstructorInfo ctor = t.GetConstructor(Type.EmptyTypes);
        //        ConnectionInfo ci = ctor.Invoke(null) as ConnectionInfo;
        //        MenuItem m = new MenuItem()
        //        {
        //            Header = ci.DatabaseType,
        //            Tag = t
        //        };
        //        l.Add(m);
        //    }
        //    buttonAdd.ContextMenu = menu;
        //}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitDataGridConnections();
            //InitButtonAddContextMenu();
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu.IsOpen = true;
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            List<ConnectionInfo> selected = new List<ConnectionInfo>();
            foreach (DataGridCellInfo cell in dataGridConnections.SelectedCells)
            {
                ConnectionInfo info = cell.Item as ConnectionInfo;
                if (info == null)
                {
                    continue;
                }
                selected.Add(info);
            }
            selected.Sort();
            for (int i = selected.Count - 1; 0 < i; i--)
            {
                if (selected[i] == selected[i - 1])
                {
                    selected.RemoveAt(i);
                }
            }
            if (selected.Count == 0)
            {
                return;
            }
            StringBuilder buf = new StringBuilder();
            foreach (ConnectionInfo info in selected)
            {
                buf.Append(info.Description);
                buf.Append(' ');
            }
            buf.Append((string)Resources["messageDelete"]);
            MessageBoxResult ret = MessageBox.Show(buf.ToString(), Properties.Resources.MessageBoxCaption_Drop, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
            foreach (ConnectionInfo info in selected)
            {
                App.Connections.Delete(info);
            }
            App.Connections.Load();
            dataGridConnections.ItemsSource = null;
            dataGridConnections.ItemsSource = App.Connections;
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(Close, DispatcherPriority.ApplicationIdle);
        }
    }
}
