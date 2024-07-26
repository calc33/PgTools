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

        private Operation _addField;
        private Operation _deleteField;
        private Operation AddField
        {
            get
            {
                if (_addField != null)
                {
                    return _addField;
                }
                _addField = new Operation((string)FindResource("AddFieldText"));
                return _addField;
            }
        }

        private Operation DeleteField
        {
            get
            {
                if (_deleteField != null)
                {
                    return _deleteField;
                }
                _deleteField = new Operation((string)FindResource("DeleteFieldText"));
                return _deleteField;
            }
        }

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
        public SortOrder Order
        {
            get
            {
                return (SortOrder)comboBoxOrder.SelectedValue;
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
                list.Add(AddField);
            }
            list.AddRange(Columns);
            if (SelectedField != null)
            {
                list.Add(DeleteField);
            }
            comboBoxField.ItemsSource = list;
            comboBoxField.SelectedItem = (SelectedField != null) ? SelectedField : (object)AddField;
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
            owner?.AddNewFieldControl(null, SortOrder.Asc);
        }
        private void UpdateComboBoxFieldSelection()
        {
            if (comboBoxField.SelectedItem == AddField)
            {
                return;
            }
            if (comboBoxField.SelectedItem == DeleteField)
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
            if (cb.SelectedItem == DeleteField)
            {
                return;
            }
            UpdateComboBoxFieldSelection();
        }
        public class Operation
        {
            public string Name { get; set; }
            public Operation(string name)
            {
                Name = name;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Operation))
                {
                    return false;
                }
                return string.Equals(Name, ((Operation)obj).Name);
            }
            public override int GetHashCode()
            {
                return Name != null ? Name.GetHashCode() : 0;
            }
            public override string ToString()
            {
                return Name;
            }
        }
    }
}
