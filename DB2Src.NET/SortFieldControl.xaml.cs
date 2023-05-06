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
    /// SortFieldControl.xaml の相互作用ロジック
    /// </summary>
    public partial class SortFieldControl: UserControl
    {
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register("Columns", typeof(ColumnCollection), typeof(SortFieldControl), new PropertyMetadata(new PropertyChangedCallback(OnColumnsPropertyChanged)));
        public static readonly DependencyProperty SelectedFieldProperty = DependencyProperty.Register("SelectedField", typeof(Column), typeof(SortFieldControl), new PropertyMetadata(new PropertyChangedCallback(OnSelectedFieldPropertyChanged)));

        public ColumnCollection Columns
        {
            get
            {
                return (ColumnCollection)GetValue(ColumnsProperty);
            }
            set
            {
                SetValue(ColumnsProperty, value);
            }
        }

        public Column SelectedField
        {
            get
            {
                return (Column)GetValue(SelectedFieldProperty);
            }
            set
            {
                SetValue(SelectedFieldProperty, value);
            }
        }
        public Order Order
        {
            get
            {
                return (Order)comboBoxOrder.SelectedValue;
            }
            set
            {
                comboBoxOrder.SelectedValue = value;
            }
        }

        private SortFieldListControl FindOwner()
        {
            FrameworkElement p = Parent as FrameworkElement;
            while (p != null)
            {
                if (p is SortFieldListControl)
                {
                    return (SortFieldListControl)p;
                }
                p = p.Parent as FrameworkElement;
            }
            return null;
        }
        private bool _hasAddField = false;
        private void UpdateComboBoxField()
        {
            List<object> list = new List<object>();
            _hasAddField = (SelectedField == null);
            if (_hasAddField)
            {
                list.Add(Operation.AddField);
            }
            list.AddRange(Columns);
            if (SelectedField != null)
            {
                list.Add(Operation.DeleteField);
            }
            comboBoxField.ItemsSource = list;
            comboBoxField.SelectedItem = (SelectedField != null) ? SelectedField : (object)Operation.AddField;
        }
        private void Unlink()
        {
            StackPanel p = (Parent as StackPanel);
            if (p != null)
            {
                p.Children.Remove(this);
            }
        }
        private void AddNewFieldControl()
        {
            SortFieldListControl owner = FindOwner();
            owner?.AddNewFieldControl(null, Order.Asc);
        }
        private void UpdateComboBoxFieldSelection()
        {
            if (comboBoxField.SelectedItem == Operation.AddField)
            {
                return;
            }
            if (comboBoxField.SelectedItem == Operation.DeleteField)
            {
                Unlink();
                return;
            }
            Column c = comboBoxField.SelectedItem as Column;
            if (SelectedField != c)
            {
                SelectedField = c;
            }
        }

        private void OnSelectedFieldPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (SelectedField != null)
            {
                if (comboBoxField.SelectedItem != SelectedField)
                {
                    comboBoxField.SelectedItem = SelectedField;
                }
                if (_hasAddField)
                {
                    AddNewFieldControl();
                }
                UpdateComboBoxField();
            }
        }

        private static void OnSelectedFieldPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as SortFieldControl)?.OnSelectedFieldPropertyChanged(e);
        }

        private void OnColumnsPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateComboBoxField();
        }

        private static void OnColumnsPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as SortFieldControl)?.OnColumnsPropertyChanged(e);
        }

        public SortFieldControl()
        {
            InitializeComponent();
        }

        private void comboBoxField_DropDownClosed(object sender, EventArgs e)
        {
            UpdateComboBoxFieldSelection();
        }

        private void comboBoxField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb == null || cb.IsDropDownOpen)
            {
                return;
            }
            if (cb.SelectedItem == Operation.DeleteField)
            {
                return;
            }
            UpdateComboBoxFieldSelection();
        }
    }
    public class Operation
    {
        public string Name { get; set; }
        public Operation(string name)
        {
            Name = name;
        }
        public static Operation AddField = new Operation("(追加)");
        public static Operation DeleteField = new Operation("(削除)");
    }
    public enum Order
    {
        Asc = 0,
        Desc = 1
    }
    public class OrderItem
    {
        public Order Value { get; set; }
        public string Text { get; set; }
        public string ToolTip { get; set; }
        public OrderItem() { Value = Order.Asc; }
        public OrderItem(Order value, string name, string tooltip)
        {
            Value = value;
            Text = name;
            ToolTip = tooltip;
        }
        public override string ToString()
        {
            return Text;
        }
        public override bool Equals(object obj)
        {
            if (obj is OrderItem)
            {
                return Value == ((OrderItem)obj).Value;
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
