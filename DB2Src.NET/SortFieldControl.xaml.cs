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
        public static readonly DependencyProperty IsMovableProperty = DependencyProperty.Register("IsMovable", typeof(bool), typeof(SortFieldControl), new PropertyMetadata(true));

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

        public bool IsMovable
        {
            get { return (bool)GetValue(IsMovableProperty); }
            set { SetValue(IsMovableProperty, value); }
        }

        public bool IsMoving
        {
            get
            {
                SortFieldListControl c = FindOwner();
                if (c is null)
                {
                    throw new ApplicationException();
                }
                return c.MovingControl == this;
            }
            set
            {
                SortFieldListControl c = FindOwner();
                if (c is null)
                {
                    throw new ApplicationException();
                }
                if (value)
                {
                    c.MovingControl = this;
                }
                else
                {
                    if (c.MovingControl == this)
                    {
                        c.MovingControl = null;
                    }
                }
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

        private void userControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetBinding(IsMovableProperty, new Binding("SelectedField") { Source = this, Converter = new NotNullToBooleanConverter() });
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

        private void gridGripper_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MouseDevice.Captured is null)
            {
                IsMoving = true;
                if (!e.MouseDevice.Capture(gridGripper))
                {
                    IsMoving = false;
                }
            }
        }

        private void gridGripper_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MouseDevice.Captured == gridGripper)
            {
                e.MouseDevice.Capture(null);
                IsMoving = false;
            }
        }

        private void gridGripper_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.MouseDevice.Captured is null)
            {
                return;
            }
            SortFieldListControl parent = FindOwner();
            if (parent is null)
            {
                e.MouseDevice.Capture(null);
                return;
            }
            SortFieldControl target = parent.MovingControl;
            if (target is null)
            {
                e.MouseDevice.Capture(null);
                return;
            }
            Point pt = e.MouseDevice.GetPosition(parent);
            SortFieldControl moveTo = App.FindVisualParent<SortFieldControl>(parent.InputHitTest(pt) as DependencyObject);
            if (0 < pt.X && pt.X < parent.ActualWidth && (moveTo is null || !moveTo.IsMovable || moveTo == target))
            {
                return;
            }
            UIElementCollection list = parent.stackPanelMain.Children;
            int i0 = list.IndexOf(target);
            int i1;
            if (moveTo != null)
            {
                i1 = list.IndexOf(moveTo);

            }
            else if (pt.X <= 8)
            {
                i1 = -1;
            }
            else
            {
                i1 = list.Count;
            }
            if (i1 < i0 - 1)
            {
                for (i1++; i1 < i0 && !(list[i1] as SortFieldControl).IsMovable; i1++) ;
            }
            else if (i0 + 1 < i1)
            {
                for (i1--; i0 < i1 && !(list[i1] as SortFieldControl).IsMovable; i1--) ;
            }
            i1 = Math.Min(Math.Max(0, i1), list.Count - 1);
            if (i0 != i1)
            {
                list.Remove(target);
                list.Insert(i1, target);
            }
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
