using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Db2Source
{
    public class CellInfo : DependencyObject
    {
        public static readonly DependencyProperty CellProperty = DependencyProperty.Register("Cell", typeof(DataGridCell), typeof(CellInfo), new PropertyMetadata(new PropertyChangedCallback(OnCellPropertyChanged)));
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register("Item", typeof(object), typeof(CellInfo), new PropertyMetadata(new PropertyChangedCallback(OnItemPropertyChanged)));
        public static readonly DependencyProperty ColumnProperty = DependencyProperty.Register("Column", typeof(ColumnInfo), typeof(CellInfo), new PropertyMetadata(new PropertyChangedCallback(OnColumnPropertyChanged)));
        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register("Index", typeof(int), typeof(CellInfo), new PropertyMetadata(new PropertyChangedCallback(OnIndexPropertyChanged)));
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(CellInfo));
        public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register("IsModified", typeof(bool), typeof(CellInfo));
        public static readonly DependencyProperty IsFaultProperty = DependencyProperty.Register("IsFault", typeof(bool), typeof(CellInfo));
        public static readonly DependencyProperty IsNullableProperty = DependencyProperty.Register("IsNullable", typeof(bool), typeof(CellInfo));
        public static readonly DependencyProperty IsNullProperty = DependencyProperty.Register("IsNull", typeof(bool), typeof(CellInfo));
        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register("TextAlignment", typeof(TextAlignment), typeof(CellInfo));
        public static readonly DependencyProperty IsCurrentRowProperty = DependencyProperty.Register("IsCurrentRow", typeof(bool), typeof(CellInfo));
        public static readonly DependencyProperty RefButtonVisibilityProperty = DependencyProperty.Register("RefButtonVisibility", typeof(Visibility), typeof(CellInfo));

        public CellInfo() : base()
        {
            BindingOperations.SetBinding(this, ItemProperty, new Binding("Cell.DataContext") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });
            BindingOperations.SetBinding(this, ColumnProperty, new Binding("Cell.Column.Header") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });
            BindingOperations.SetBinding(this, IndexProperty, new Binding("Column.Index") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });
        }

        public DataGridCell Cell
        {
            get
            {
                return (DataGridCell)GetValue(CellProperty);
            }
            set
            {
                SetValue(CellProperty, value);
            }
        }
        public Row Row
        {
            get
            {
                return Item as Row;
            }
            private set
            {
                Item = value;
            }
        }
        public object Item
        {
            get
            {
                return GetValue(ItemProperty);
            }
            private set
            {
                SetValue(ItemProperty, value);
            }
        }
        public ColumnInfo Column
        {
            get
            {
                return (ColumnInfo)GetValue(ColumnProperty);
            }
            private set
            {
                SetValue(ColumnProperty, value);
            }
        }
        private string _indexPropertyName;

        private void InvalidateIndexPropertyName()
        {
            _indexPropertyName = null;
        }

        private void UpdateIndexPropertyName()
        {
            if (_indexPropertyName != null)
            {
                return;
            }
            _indexPropertyName = string.Format("[{0}]", Index);
        }

        private string IndexPropertyName
        {
            get
            {
                UpdateIndexPropertyName();
                return _indexPropertyName;
            }
        }

        public int Index
        {
            get
            {
                return (int)GetValue(IndexProperty);
            }
            private set
            {
                SetValue(IndexProperty, value);
            }
        }
        public object Data
        {
            get
            {
                return GetValue(DataProperty);
            }
            set
            {
                SetValue(DataProperty, value);
            }
        }
        public object Old
        {
            get
            {
                return Row?.Old(Index);
            }
        }
        public bool IsModified
        {
            get
            {
                return (bool)GetValue(IsModifiedProperty);
            }
            private set
            {
                SetValue(IsModifiedProperty, value);
            }
        }
        public bool IsFault
        {
            get
            {
                return (bool)GetValue(IsFaultProperty);
            }
            private set
            {
                SetValue(IsFaultProperty, value);
            }
        }
        public bool IsNullable
        {
            get
            {
                return (bool)GetValue(IsNullableProperty);
            }
            private set
            {
                SetValue(IsNullableProperty, value);
            }
        }
        public bool IsNull
        {
            get
            {
                return (bool)GetValue(IsNullProperty);
            }
            private set
            {
                SetValue(IsNullProperty, value);
            }
        }

        public TextAlignment TextAlignment
        {
            get
            {
                return (TextAlignment)GetValue(TextAlignmentProperty);
            }
            private set
            {
                SetValue(TextAlignmentProperty, value);
            }
        }

        public bool IsCurrentRow
        {
            get
            {
                return (bool)GetValue(IsCurrentRowProperty);
            }
            set
            {
                SetValue(IsCurrentRowProperty, value);
            }
        }

        public Visibility RefButtonVisibility
        {
            get
            {
                return (Visibility)GetValue(RefButtonVisibilityProperty);
            }
            set
            {
                SetValue(RefButtonVisibilityProperty, value);
            }
        }

        private DataGrid _grid;
        public void UpdateCurrentRow()
        {
            if (_grid == null)
            {
                return;
            }
            IsCurrentRow = (_grid.CurrentCell.Item as Row == Row);
        }
        private void UpdateData()
        {
            if (Row == null || Column == null)
            {
                Data = null;
                IsModified = false;
                IsFault = false;
                IsNullable = true;
                IsNull = false;
                TextAlignment = TextAlignment.Left;
                RefButtonVisibility = Visibility.Collapsed;
                return;
            }
            object o = Row[Index];
            Data = o;
            IsModified = Row.IsModified(Index);
            IsNullable = Column.IsNullable;
            IsNull = (o == null) || (o is DBNull);
            IsFault = Column.IsNotNull && IsNull;
            TextAlignment = (Column.IsNumeric || Column.IsDateTime) ? TextAlignment.Right : TextAlignment.Left;
            bool hasRef = Column.ForeignKeys != null && Column.ForeignKeys.Length != 0;
            RefButtonVisibility = (hasRef && !IsNull) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Row_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == IndexPropertyName)
            {
                UpdateData();
            }
        }

        private void OnItemPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
            {
                return;
            }
            Row row = e.OldValue as Row;
            if (row != null)
            {
                row.PropertyChanged -= Row_PropertyChanged;
            }
            row = e.NewValue as Row;
            if (row != null)
            {
                row.PropertyChanged += Row_PropertyChanged;
            }
            UpdateData();
        }

        private static void OnItemPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as CellInfo).OnItemPropertyChanged(e);
        }

        private void OnColumnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
            {
                return;
            }
            UpdateData();
        }

        private static void OnColumnPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as CellInfo).OnColumnPropertyChanged(e);
        }

        private void OnIndexPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
            {
                return;
            }
            InvalidateIndexPropertyName();
            UpdateData();
        }

        private static void OnIndexPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as CellInfo).OnIndexPropertyChanged(e);
        }

        private void OnCellPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DataGridCell cell = e.NewValue as DataGridCell;
            _grid = App.FindVisualParent<DataGrid>(cell);
            UpdateData();
        }

        private static void OnCellPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as CellInfo).OnCellPropertyChanged(e);
        }
    }
}
