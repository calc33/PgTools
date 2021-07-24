using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinForm = System.Windows.Forms;

namespace Db2Source
{
    public class ArrayFormatProvider: IFormatProvider, ICustomFormatter
    {
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg == null)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append('(');
            bool needComma = false;
            foreach (object o in (IEnumerable)arg)
            {
                if (needComma)
                {
                    buf.Append(", ");
                }
                buf.Append(o);
                needComma = true;
            }
            buf.Append(')');
            return buf.ToString();
        }

        public object GetFormat(Type formatType)
        {
            if (formatType.IsArray || formatType.IsSubclassOf(typeof(IEnumerable)))
            {
                return this;
            }
            return null;
        }
    }
    public class CellValueChangedEventArgs: EventArgs
    {
        public Row Row { get; private set; }
        public int Index { get; private set; }
        internal CellValueChangedEventArgs(Row row, int index)
        {
            Row = row;
            Index = index;
        }
    }
    public class RowChangedEventArgs: EventArgs
    {
        public Row Row { get; private set; }
        internal RowChangedEventArgs(Row row)
        {
            Row = row;
        }
    }

    public sealed class NotSupportedColumn
    {
        public static readonly NotSupportedColumn Value = new NotSupportedColumn();
        public override string ToString()
        {
            return "(NOT SUPPORTED)";
        }
    }

    public sealed class OverflowColumn
    {
        public static readonly NotSupportedColumn Value = new NotSupportedColumn();
        public override string ToString()
        {
            return "(OVERFLOW)";
        }
    }

    public static class DataGridCommands
    {
        public static readonly RoutedCommand SelectAllCells = InitSelectAllCells();
        public static readonly RoutedCommand CopyTable = InitCopyTable();
        public static readonly RoutedCommand CopyTableContent = InitCopyTableContent();
        public static readonly RoutedCommand CopyTableAsInsert = InitCopyTableAsInsert();
        public static readonly RoutedCommand CopyTableAsCopy = InitCopyTableAsCopy();
        public static readonly RoutedCommand CheckAll = InitCheckAll();
        public static readonly RoutedCommand UncheckAll = InitUncheckAll();

        private static RoutedCommand InitSelectAllCells()
        {
            RoutedCommand ret = new RoutedCommand("表全体を選択", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
        private static RoutedCommand InitCopyTable()
        {
            RoutedCommand ret = new RoutedCommand("表をコピー", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
        private static RoutedCommand InitCopyTableContent()
        {
            RoutedCommand ret = new RoutedCommand("表をコピー(データのみ)", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
        private static RoutedCommand InitCopyTableAsInsert()
        {
            RoutedCommand ret = new RoutedCommand("表をINSERT文形式でコピー", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
        private static RoutedCommand InitCopyTableAsCopy()
        {
            RoutedCommand ret = new RoutedCommand("表をCOPY文形式でコピー", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
            return ret;
        }
        private static RoutedCommand InitCheckAll()
        {
            RoutedCommand ret = new RoutedCommand("すべてチェック", typeof(DataGrid));
            return ret;
        }
        private static RoutedCommand InitUncheckAll()
        {
            RoutedCommand ret = new RoutedCommand("すべてチェックをはずす", typeof(DataGrid));
            return ret;
        }
    }

    public enum SearchDirection
    {
        None,
        Forward,
        Backward
    }
    public class CellInfo: DependencyObject
    {
        public static readonly DependencyProperty CellProperty = DependencyProperty.Register("Cell", typeof(DataGridCell), typeof(CellInfo));
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register("Item", typeof(object), typeof(CellInfo));
        public static readonly DependencyProperty ColumnProperty = DependencyProperty.Register("Column", typeof(ColumnInfo), typeof(CellInfo));
        public static readonly DependencyProperty IndexProperty = DependencyProperty.Register("Index", typeof(int), typeof(CellInfo));
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
            if (e.PropertyName == _indexPropertyName)
            {
                UpdateData();
            }
        }

        private void ItemPropertyChanged(DependencyPropertyChangedEventArgs e)
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
        private void ColumnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
            {
                return;
            }
            UpdateData();
        }
        private void UpdateIndexPropertyName()
        {
            _indexPropertyName = string.Format("[{0}]", Index);
        }
        private void IndexPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateIndexPropertyName();
            if (e.OldValue == e.NewValue)
            {
                return;
            }
            UpdateData();
        }
        private void CellPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            DataGridCell cell = e.NewValue as DataGridCell;
            _grid = App.FindVisualParent<DataGrid>(cell);
            UpdateData();
        }
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ItemProperty)
            {
                ItemPropertyChanged(e);
            }
            if (e.Property == ColumnProperty)
            {
                ColumnPropertyChanged(e);
            }
            if (e.Property == IndexProperty)
            {
                IndexPropertyChanged(e);
            }
            if (e.Property == CellProperty)
            {
                CellPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }
    }

    public class Row: IList<object>, IComparable, IChangeSetRow, INotifyPropertyChanged
    {
        private static readonly object Unchanged = new object();
        private readonly DataGridController _owner;
        internal object[] _data;
        internal object[] _old;
        internal bool _added = false;
        internal bool _deleted = false;
        private bool? _hasChanges = null;
        internal bool _hasError = false;
        internal string _errorMessage = string.Empty;
        private void UpdateHasChanges()
        {
            if (_hasChanges.HasValue)
            {
                return;
            }
            try
            {
                if (_added || _deleted)
                {
                    _hasChanges = true;
                    return;
                }
                if (_hasChanges.HasValue)
                {
                    return;
                }
                for (int i = 0; i < _data.Length; i++)
                {
                    if (IsModified(i))
                    {
                        _hasChanges = true;
                        return;
                    }
                }
                _hasChanges = false;
            }
            finally
            {
                _owner.InvalidateIsModified();
            }
        }
        private void InvalidateHasChanges()
        {
            _hasChanges = null;
            OnPropertyChanged("HasChanges");
        }
        public void AcceptChanges()
        {
            for (int i = 0; i < _old.Length; i++)
            {
                _old[i] = Unchanged;
            }
            _hasChanges = false;
            _added = false;
            if (_owner != null)
            {
                _owner.Rows.InvalidateKeyToRow();
                if (_deleted)
                {
                    _owner.Rows.Remove(this);
                }
            }
            _deleted = false;
            HasError = false;
            ErrorMessage = null;
            OnPropertyChanged("IsDeleted");
            OnPropertyChanged("HasChanges");
        }
        public void RevertChanges()
        {
            for (int i = 0; i < _old.Length; i++)
            {
                if (_old[i] != Unchanged)
                {
                    _data[i] = _old[i];
                    _old[i] = Unchanged;
                    OnValueChanged(i);
                }
            }
            if (_added && _owner != null)
            {
                _owner.Rows.Remove(this);
            }
            _hasChanges = false;
            _deleted = false;
            HasError = false;
            ErrorMessage = null;
        }
        public void ClearError()
        {
            HasError = false;
            ErrorMessage = null;
        }
        public void SetError(Exception t)
        {
            SetError(_owner.DataSet.GetExceptionMessage(t));
        }
        public void SetError(string message)
        {
            HasError = true;
            ErrorMessage = message;
        }


        public bool IsDeleted
        {
            get
            {
                return _deleted;
            }
            set
            {
                if (_deleted == value)
                {
                    return;
                }
                _deleted = value;
                OnPropertyChanged("IsDeleted");
                InvalidateHasChanges();
                if (_deleted)
                {
                    _owner?.OnRowDeleted(new RowChangedEventArgs(this));
                }
                else
                {
                    _owner?.OnRowUndeleted(new RowChangedEventArgs(this));
                }
            }
        }

        public bool HasChanges
        {
            get
            {
                UpdateHasChanges();
                return _hasChanges.HasValue && _hasChanges.Value;
            }
        }

        public bool HasError
        {
            get
            {
                return _hasError;
            }
            private set
            {
                if (_hasError == value)
                {
                    return;
                }
                _hasError = value;
                OnPropertyChanged("HasError");
                Owner.InvalidateHasError();
            }
        }

        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
            private set
            {
                if (_errorMessage == value)
                {
                    return;
                }
                _errorMessage = value;
                OnPropertyChanged("ErrorMessage");
            }
        }

        /// <summary>
        /// DeletedRowsに追加する必要がある場合はtrueを返す
        /// </summary>
        /// <returns></returns>
        internal bool BecomeDeleted()
        {
            IsDeleted = true;
            return !_added;
        }
        internal void BecomeUndeleted()
        {
            IsDeleted = false;
        }

        private static ChangeKind[,,] InitFlagsToChangeKind()
        {
            // 添え字は _added, _deleted, _hasChanges をfalse=0,true=1に変換して割り当てる
            ChangeKind[,,] ret = new ChangeKind[2, 2, 2];
            ret[0, 0, 0] = ChangeKind.None;
            ret[0, 0, 1] = ChangeKind.Modify;
            ret[0, 1, 0] = ChangeKind.Delete;
            ret[0, 1, 1] = ChangeKind.Delete;
            ret[1, 0, 0] = ChangeKind.New;
            ret[1, 0, 1] = ChangeKind.New;
            ret[1, 1, 0] = ChangeKind.None;
            ret[1, 1, 1] = ChangeKind.None;
            return ret;
        }
        private static readonly ChangeKind[,,] FlagsToChangeKind = InitFlagsToChangeKind();
        public ChangeKind ChangeKind
        {
            get
            {
                UpdateHasChanges();
                return FlagsToChangeKind[_added ? 1 : 0, _deleted ? 1 : 0, (_hasChanges.HasValue && _hasChanges.Value) ? 1 : 0];
            }
        }

        internal void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// i番目の項目が変更されていれば IsModified(i) が true を返す
        /// </summary>
        public bool IsModified(int index)
        {
            if (index == -1)
            {
                return false;
            }
            return _old[index] != Unchanged && !Equals(_old[index], _data[index]);
        }
        public bool IsAdded
        {
            get
            {
                return _added;
            }
        }
        public bool IsChecked { get; set; }
        /// <summary>
        /// Old[i] で i番目の項目の読み込み時の値を返す
        /// </summary>
        public object Old(int index)
        {
            object ret = _old[index];
            if (ret != Unchanged)
            {
                return ret;
            }
            return _data[index];
        }

        public DataGridController Owner
        {
            get
            {
                return _owner;
            }
        }

        public object[] GetKeys()
        {
            if (_owner == null || _owner.KeyFields == null || _owner.KeyFields.Length == 0)
            {
                return _data;
            }
            object[] ret = new object[_owner.KeyFields.Length];
            for (int i = 0; i < _owner.KeyFields.Length; i++)
            {
                ColumnInfo f = _owner.KeyFields[i];
                ret[i] = _data[f.Index];
            }
            return ret;
        }
        public object[] GetOldKeys()
        {
            if (_owner == null || _owner.KeyFields == null || _owner.KeyFields.Length == 0)
            {
                return _data;
            }
            object[] ret = new object[_owner.KeyFields.Length];
            for (int i = 0; i < _owner.KeyFields.Length; i++)
            {
                int p = _owner.KeyFields[i].Index;
                object o = (0 <= p && p < _old.Length) ? _old[p] : null;
                if (o == Unchanged)
                {
                    o = _data[p];
                }
                ret[i] = o;
            }
            return ret;
        }

        public ColumnInfo[] GetForeignKeyColumns(ForeignKeyConstraint constraint)
        {
            if (constraint == null)
            {
                throw new ArgumentNullException("constraint");
            }
            if (_owner == null)
            {
                return null;
            }
            return _owner.GetForeignKeyColumns(constraint);
        }

        private void OnValueChanged(int index)
        {
            _owner?.OnCellValueChanged(new CellValueChangedEventArgs(this, index));
            OnPropertyChanged(string.Format("[{0}]", index));
            Owner.Rows.OnRowChanged(this);
            InvalidateHasChanges();
        }

        #region IList<T>の実装
        public object this[int index]
        {
            get
            {
                return _data[index];
            }
            set
            {
                if (_old[index] == Unchanged)
                {
                    _old[index] = _data[index];
                }
                object v = _owner.Fields[index].ConvertValue(value);
                _data[index] = v;
                OnValueChanged(index);
            }
        }
        public int Count
        {
            get
            {
                return ((ICollection<object>)_data).Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(object item) { }
        public void Clear() { }

        public bool Contains(object item)
        {
            return ((ICollection<object>)_data).Contains(item);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            ((ICollection<object>)_data).CopyTo(array, arrayIndex);
        }

        public bool Remove(object item)
        {
            return false;
        }

        public IEnumerator<object> GetEnumerator()
        {
            return ((ICollection<object>)_data).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<object>)_data).GetEnumerator();
        }

        public int IndexOf(object item)
        {
            for (int i = 0; i < _data.Length; i++)
            {
                if (object.Equals(_data[i], item))
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, object item) { }
        public void RemoveAt(int index) { }
        #endregion

        public void Read(IDataReader reader, int[] indexList)
        {
            object[] work = new object[reader.FieldCount];
            if (!reader.Read())
            {
                return;
            }
            reader.GetValues(work);
            for (int i = 0; i < work.Length; i++)
            {
                if (indexList[i] == -1)
                {
                    continue;
                }
                _data[indexList[i]] = work[i];
            }
            for (int i = 0; i < _old.Length; i++)
            {
                _old[i] = Unchanged;
            }
        }

        //public void SetOwner(DataGridController owner)
        //{
        //    _owner = owner;
        //    _added = true;
        //    _deleted = false;
        //    _data = new object[owner.Fields.Length];
        //    _old = new object[owner.Fields.Length];
        //    int i = 0;
        //    foreach (ColumnInfo info in owner.Fields)
        //    {
        //        if (info.IsDefaultDefined && info.Column != null)
        //        {
        //            _data[i] = info.Column.EvalDefaultValue();
        //        }
        //        i++;
        //    }
        //}

        public Row(DataGridController owner, IDataReader reader)
        {
            _owner = owner;
            _added = false;
            _deleted = false;
            _data = new object[reader.FieldCount];
            _old = new object[reader.FieldCount];
            for (int i = 0; i < _old.Length; i++)
            {
                try
                {
                    _data[i] = reader.GetValue(i);

                }
                catch (NotSupportedException)
                {
                    _data[i] = NotSupportedColumn.Value;
                }
                catch (OverflowException)
                {
                    _data[i] = OverflowColumn.Value;
                }
            }
            for (int i = 0; i < _old.Length; i++)
            {
                _old[i] = Unchanged;
            }
        }

        public Row(DataGridController owner)
        {
            _owner = owner;
            _added = true;
            _deleted = false;
            _data = new object[owner.Fields.Length];
            _old = new object[owner.Fields.Length];
            int i = 0;
            foreach (ColumnInfo info in owner.Fields)
            {
                if (info.IsDefaultDefined && info.Column != null)
                {
                    _data[i] = info.Column.EvalDefaultValue();
                }
                i++;
            }
        }
        public Row()
        {
            _added = true;
            _deleted = false;
            _data = new object[0];
            _old = new object[0];
        }
        public override bool Equals(object obj)
        {
            if (!(obj is Row))
            {
                return base.Equals(obj);
            }
            if (_added)
            {
                return ReferenceEquals(this, obj);
            }
            object[] k1 = GetOldKeys();
            object[] k2 = ((Row)obj).GetOldKeys();
            if (k1.Length != k2.Length)
            {
                return false;
            }
            for (int i = 0; i < k1.Length; i++)
            {
                if (!Equals(k1[i], k2[i]))
                {
                    return false;
                }
            }
            return true;
        }
        public override int GetHashCode()
        {
            int ret = 0;
            object[] k = GetOldKeys();
            foreach (object o in k)
            {
                if (o != null)
                {
                    ret *= 13;
                    ret += o.GetHashCode();
                }
            }
            return ret;
        }
        public override string ToString()
        {
            if (_owner == null || _owner.KeyFields == null || _owner.KeyFields.Length == 0)
            {
                return base.ToString();
            }
            StringBuilder buf = new StringBuilder();
            buf.Append('{');
            bool needComma = false;
            foreach (ColumnInfo f in _owner.KeyFields)
            {
                object o = (f != null) ? _data[f.Index] : null;
                buf.Append(o != null ? o.ToString() : "(null)");
                if (needComma)
                {
                    buf.Append(',');
                }
                needComma = true;
            }
            buf.Append('}');
            return buf.ToString();
        }

        public int CompareTo(object obj)
        {
            if (!(obj is Row))
            {
                return -1;
            }
            return CompareRowByOldKey(this, (Row)obj);
        }
        public static int CompareRowByKey(Row item1, Row item2)
        {
            if (item1 == null || item2 == null)
            {
                return ((item1 == null) ? 1 : 0) - ((item2 == null) ? 1 : 0);
            }
            return CompareKey(item1.GetKeys(), item2.GetKeys());
        }

        public static int CompareRowByOldKey(Row item1, Row item2)
        {
            if (item1 == null || item2 == null)
            {
                return ((item1 == null) ? 1 : 0) - ((item2 == null) ? 1 : 0);
            }
            return CompareKey(item1.GetOldKeys(), item2.GetOldKeys());
        }
        public static int CompareKey(object[] item1, object[] item2)
        {
            if (item1 == null || item2 == null)
            {
                return ((item1 == null) ? 1 : 0) - ((item2 == null) ? 1 : 0);
            }
            if (item1.Length != item2.Length)
            {
                return item1.Length - item2.Length;
            }
            for (int i = 0; i < item1.Length; i++)
            {
                object o1 = item1[i];
                object o2 = item2[i];
                int ret;
                if ((o1 == null) || (o2 == null))
                {
                    ret = ((o1 == null) ? 1 : 0) - ((o2 == null) ? 1 : 0);
                    if (ret != 0)
                    {
                        return ret;
                    }
                    continue;
                }
                if (!(o1 is IComparable))
                {
                    return -1;
                }
                ret = ((IComparable)o1).CompareTo(o2);
                if (ret != 0)
                {
                    return ret;
                }
            }
            return 0;
        }
    }

    public sealed class RowCollection: IList<Row>, IList, IChangeSetRows, INotifyPropertyChanged, INotifyCollectionChanged
    {
        private readonly DataGridController _owner;
        private readonly List<Row> _list = new List<Row>();
        private Dictionary<object[], Row> _keyToRow = null;
        private readonly Dictionary<object[], Row> _oldKeyToRow = new Dictionary<object[], Row>();
        //private List<Row> _deletedRows = new List<Row>();
        private List<Row> _temporaryRows = new List<Row>();

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void RequireKeyToRow()
        {
            if (_keyToRow != null)
            {
                return;
            }
            _keyToRow = new Dictionary<object[], Row>();
            foreach (Row row in _list)
            {
                _keyToRow[row.GetKeys()] = row;
            }
        }
        internal void InvalidateKeyToRow()
        {
            _keyToRow = null;
        }

        public Row FindRowByKey(object[] key)
        {
            RequireKeyToRow();
            Row row;
            if (!_keyToRow.TryGetValue(key, out row))
            {
                return null;
            }
            return row;
        }
        public Row FindRowByOldKey(object[] key)
        {
            Row row;
            if (!_oldKeyToRow.TryGetValue(key, out row))
            {
                return null;
            }
            return row;
        }
        IChangeSetRow IChangeSetRows.FingRowByOldKey(object[] key)
        {
            Row row;
            if (!_oldKeyToRow.TryGetValue(key, out row))
            {
                return null;
            }
            return row;
        }

        public void AcceptChanges()
        {
            // AcceptChanges()内で_listの内容を削除する可能性があるため_listを逆順サーチ
            for (int i = _list.Count - 1; 0 <= i; i--)
            {
                Row r = _list[i];
                r.AcceptChanges();
            }
            foreach (Row r in _temporaryRows)
            {
                if (_list.IndexOf(r) == -1)
                {
                    r.AcceptChanges();
                    //_list.Add(r);
                    Add(r);
                }
            }
            //_deletedRows = new List<Row>();
            _temporaryRows = new List<Row>();
            //_modifiedRowsDict = new Dictionary<object[], Row>();
        }

        public void RevertChanges()
        {
            for (int i = _list.Count - 1; 0 <= i; i--)
            {
                Row item = _list[i];
                switch (item.ChangeKind)
                {
                    case ChangeKind.New:
                        _list.RemoveAt(i);
                        break;
                    case ChangeKind.Modify:
                    case ChangeKind.Delete:
                        item.RevertChanges();
                        break;
                }
            }
            _temporaryRows.Clear();
        }

        internal void TrimDeletedRows()
        {
            for (int i = _list.Count - 1; 0 <= i; i--)
            {
                Row row = _list[i];
                if (row._added && row._deleted)
                {
                    //_list.RemoveAt(i);
                    RemoveAt(i);
                }
            }
        }

        public ICollection<Row> TemporaryRows
        {
            get { return _temporaryRows; }
        }
        ICollection<IChangeSetRow> IChangeSetRows.TemporaryRows
        {
            get { return (ICollection<IChangeSetRow>)_temporaryRows; }
        }

        internal RowCollection(DataGridController owner)
        {
            _owner = owner;
        }

        #region interfaceの実装
        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return ((IList)_list).SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((IList)_list).IsSynchronized;
            }
        }

        object IList.this[int index]
        {
            get
            {
                return ((IList)_list)[index];
            }

            set
            {
                if (((IList)_list)[index] == value)
                {
                    return;
                }
                ((IList)_list)[index] = value;
                OnPropertyChanged(string.Format("[{0}]", index));
            }
        }

        public Row this[int index]
        {
            get
            {
                return _list[index];
            }

            set
            {
                if (_list[index] == value)
                {
                    return;
                }
                _list[index] = value;
                OnPropertyChanged(string.Format("[{0}]", index));
            }
        }

        public int IndexOf(Row item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, Row item)
        {
            _list.Insert(index, item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            InvalidateKeyToRow();
            if (item.ChangeKind != ChangeKind.New)
            {
                _oldKeyToRow.Add(item.GetOldKeys(), item);
            }
            item.BecomeUndeleted();
            _owner?.OnRowAdded(new RowChangedEventArgs(item));
        }

        public void RemoveAt(int index)
        {
            if (index == -1)
            {
                return;
            }
            Row item = _list[index];
            _list.RemoveAt(index);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            InvalidateKeyToRow();
        }

        public void Add(Row item)
        {
            _list.Add(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            InvalidateKeyToRow();
            if (item.ChangeKind != ChangeKind.New)
            {
                _oldKeyToRow.Add(item.GetOldKeys(), item);
            }
            item.BecomeUndeleted();
            _owner?.OnRowAdded(new RowChangedEventArgs(item));
        }

        public void Clear()
        {
            _list.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            InvalidateKeyToRow();
            _oldKeyToRow.Clear();
        }

        public bool Contains(Row item)
        {
            return ((IList<Row>)_list).Contains(item);
        }

        public void CopyTo(Row[] array, int arrayIndex)
        {
            ((IList<Row>)_list).CopyTo(array, arrayIndex);
        }

        public bool Remove(Row item)
        {
            bool f1 = _list.Remove(item);
            if (f1)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            }
            bool f2 = _temporaryRows.Remove(item);
            if (!f1 && !f2)
            {
                return false;
            }
            InvalidateKeyToRow();
            return f1;
        }

        public IEnumerator<Row> GetEnumerator()
        {
            return ((IList<Row>)_list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<Row>)_list).GetEnumerator();
        }

        public int Add(object value)
        {
            if (!(value is Row))
            {
                throw new ArgumentException("value");
            }
            int ret = ((IList)_list).Add(value);
            Row row = (Row)value;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, row));
            InvalidateKeyToRow();
            if (row.ChangeKind != ChangeKind.New)
            {
                _oldKeyToRow.Add(row.GetOldKeys(), row);
            }
            row.BecomeUndeleted();
            _owner?.OnRowAdded(new RowChangedEventArgs(row));
            return ret;

        }

        public bool Contains(object value)
        {
            return ((IList)_list).Contains(value);
        }

        public int IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            if (!(value is Row))
            {
                throw new ArgumentException("value");
            }
            ((IList)_list).Insert(index, value);
            Row row = (Row)value;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, row));
            InvalidateKeyToRow();
            if (row.ChangeKind != ChangeKind.New)
            {
                _oldKeyToRow.Add(row.GetOldKeys(), row);
            }
            row.BecomeUndeleted();
            _owner?.OnRowAdded(new RowChangedEventArgs(row));
        }

        public void Remove(object value)
        {
            Row item = value as Row;
            int i = _list.IndexOf(item);
            if (i == -1)
            {
                return;
            }
            Row row = _list[i];
            _list.RemoveAt(i);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, row));
            InvalidateKeyToRow();
            return;
        }

        internal void OnRowChanged(Row row)
        {
            int i = IndexOf(row);
            if (i == -1)
            {
                return;
            }
            OnPropertyChanged(string.Format("[{0}]", i));
        }
        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }
        #endregion
    }

    public class DataGridController: DependencyObject, IChangeSet
    {
        public static readonly DependencyProperty GridProperty = DependencyProperty.Register("Grid", typeof(DataGrid), typeof(DataGridController));
        public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register("IsModified", typeof(bool), typeof(DataGridController));
        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register("SearchText", typeof(string), typeof(DataGridController));
        public static readonly DependencyProperty IgnoreCaseProperty = DependencyProperty.Register("IgnoreCase", typeof(bool), typeof(DataGridController));
        public static readonly DependencyProperty WordwrapProperty = DependencyProperty.Register("Wordwrap", typeof(bool), typeof(DataGridController));
        public static readonly DependencyProperty UseRegexProperty = DependencyProperty.Register("UseRegex", typeof(bool), typeof(DataGridController));
        public static readonly DependencyProperty UseSearchColumnProperty = DependencyProperty.Register("UseSearchColumn", typeof(bool), typeof(DataGridController));
        public static readonly DependencyProperty SearchColumnProperty = DependencyProperty.Register("SearchColumn", typeof(DataGridColumn), typeof(DataGridController));
        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register("Rows", typeof(RowCollection), typeof(DataGridController));
        public static readonly DependencyProperty HasErrorProperty = DependencyProperty.Register("HasError", typeof(bool), typeof(DataGridController));

        public static readonly DependencyProperty CellInfoProperty = DependencyProperty.RegisterAttached("CellInfo", typeof(CellInfo), typeof(DataGridController));
        public static readonly DependencyProperty GridControllerProperty = DependencyProperty.RegisterAttached("GridController", typeof(DataGridController), typeof(DataGridController));

        public static CellInfo GetCellInfo(DependencyObject obj)
        {
            return (CellInfo)obj.GetValue(CellInfoProperty);
        }
        public static void SetCellInfo(DependencyObject obj, CellInfo value)
        {
            obj.SetValue(CellInfoProperty, value);
        }

        public static DataGridController GetGridController(DependencyObject obj)
        {
            return (DataGridController)obj.GetValue(GridControllerProperty);
        }
        public static void SetGridController(DependencyObject obj, DataGridController value)
        {
            obj.SetValue(GridControllerProperty, value);
        }

        public DataGrid Grid
        {
            get
            {
                return (DataGrid)GetValue(GridProperty);
            }
            set
            {
                SetValue(GridProperty, value);
            }
        }

        private Dictionary<DataGridColumn, int> _columnToDataIndex = null;

        public bool IsModified
        {
            get
            {
                return (bool)GetValue(IsModifiedProperty);
            }
            set
            {
                SetValue(IsModifiedProperty, value);
            }
        }

        public string SearchText
        {
            get
            {
                return (string)GetValue(SearchTextProperty);
            }
            set
            {
                SetValue(SearchTextProperty, value);
            }
        }

        public bool IgnoreCase
        {
            get
            {
                return (bool)GetValue(IgnoreCaseProperty);
            }
            set
            {
                SetValue(IgnoreCaseProperty, value);
            }
        }

        public bool Wordwrap
        {
            get
            {
                return (bool)GetValue(WordwrapProperty);
            }
            set
            {
                SetValue(WordwrapProperty, value);
            }
        }

        public bool UseRegex
        {
            get
            {
                return (bool)GetValue(UseRegexProperty);
            }
            set
            {
                SetValue(UseRegexProperty, value);
            }
        }

        public bool UseSearchColumn
        {
            get
            {
                return (bool)GetValue(UseSearchColumnProperty);
            }
            set
            {
                SetValue(UseSearchColumnProperty, value);
            }
        }

        public DataGridColumn SearchColumn
        {
            get
            {
                return (DataGridColumn)GetValue(SearchColumnProperty);
            }
            set
            {
                SetValue(SearchColumnProperty, value);
            }
        }

        /// <summary>
        /// Grid中の選択されたセルのうち
        /// ascend=true の場合は先頭のセルを返す
        /// ascend=falseの場合は末尾のセルを返す
        /// どのセルも選択されていない場合はnullを返す
        /// </summary>
        /// <param name="ascend"></param>
        /// <returns></returns>
        public DataGridCellInfo? GetSelectedCell(bool ascend)
        {
            IList<DataGridCellInfo> l = Grid.SelectedCells;
            if (0 < l.Count)
            {
                return ascend ? l.First() : l.Last();
            }
            return null;
        }

        public bool GetCurentCellPosition(bool searchForward, out int row, out int column)
        {
            row = -1;
            column = -1;
            if (Grid == null)
            {
                return false;
            }
            DataGridCellInfo? info = GetSelectedCell(searchForward);
            if (!info.HasValue || !info.Value.IsValid)
            {
                return false;
            }
            row = Grid.Items.IndexOf(info.Value.Item);
            column = info.Value.Column.DisplayIndex;
            return true;
        }

        private bool _isSearchEndValid = false;
        private int _searchEndRow = -1;
        private int _searchEndColumn = -1;

        private void GetSearchEnd(DataGridInfo info, bool searchForward, out bool isFirst, out int endRow, out int endColumn)
        {
            if (_isSearchEndValid)
            {
                isFirst = false;
                endRow = _searchEndRow;
                endColumn = _searchEndColumn;
                return;
            }
            _isSearchEndValid = GetCurentCellPosition(searchForward, out _searchEndRow, out _searchEndColumn);
            isFirst = true;
            endRow = _searchEndRow;
            endColumn = _searchEndColumn;
            if (info.EndRow <= endRow)
            {
                endRow = info.EndRow;
                endColumn = info.EndColumn;
            }
            else if (endColumn < info.StartColumn)
            {
                endColumn = info.StartColumn;
            }
            else if (info.EndColumn < endColumn)
            {
                endColumn = info.EndColumn;
            }
        }
        private void InvalidateSearchEnd()
        {
            _isSearchEndValid = false;
        }

        private bool _updateIsModifiedPosted = false;
        internal void UpdateIsModified()
        {
            try
            {
                Rows.TrimDeletedRows();
                foreach (Row row in Rows)
                {
                    if (row.ChangeKind != ChangeKind.None)
                    {
                        IsModified = true;
                        return;
                    }
                }
                IsModified = false;
            }
            finally
            {
                _updateIsModifiedPosted = false;
            }
        }

        internal void InvalidateIsModified()
        {
            if (_updateIsModifiedPosted)
            {
                return;
            }
            _updateIsModifiedPosted = true;
            //Dispatcher.BeginInvoke((Action)UpdateIsModified);
            Dispatcher.Invoke(UpdateIsModified, DispatcherPriority.ApplicationIdle);
        }

        public event EventHandler<DependencyPropertyChangedEventArgs> GridPropertyChanged;
        protected void OnGridPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateGrid();
            GridPropertyChanged?.Invoke(this, e);
        }

        public void OnIsModifiedPropertyChanged(DependencyPropertyChangedEventArgs e)
        {

        }
        public void OnSearchTextPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateSearchEnd();
        }
        public void OnIgnoreCasePropertyChanged(DependencyPropertyChangedEventArgs e)
        {

        }
        public void OnWordwrapPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMatchTextProc();
        }
        public void OnUseRegexPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMatchTextProc();
        }
        public void OnUseSearchColumnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMatchTextProc();
        }
        public void OnSearchColumnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMatchTextProc();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == GridProperty)
            {
                OnGridPropertyChanged(e);
            }
            if (e.Property == IsModifiedProperty)
            {
                OnIsModifiedPropertyChanged(e);
            }
            if (e.Property == SearchTextProperty)
            {
                OnSearchTextPropertyChanged(e);
            }
            if (e.Property == IgnoreCaseProperty)
            {
                OnIgnoreCasePropertyChanged(e);
            }
            if (e.Property == WordwrapProperty)
            {
                OnWordwrapPropertyChanged(e);
            }
            if (e.Property == UseRegexProperty)
            {
                OnUseRegexPropertyChanged(e);
            }
            if (e.Property == UseSearchColumnProperty)
            {
                OnUseSearchColumnPropertyChanged(e);
            }
            if (e.Property == SearchColumnProperty)
            {
                OnSearchColumnPropertyChanged(e);
            }
            base.OnPropertyChanged(e);
        }
        public event EventHandler<CellValueChangedEventArgs> CellValueChanged;
        protected internal void OnCellValueChanged(CellValueChangedEventArgs e)
        {
            if (e.Row.ChangeKind != ChangeKind.None)
            {
                IsModified = true;
            }
            CellValueChanged?.Invoke(this, e);
        }
        public event EventHandler<RowChangedEventArgs> RowAdded;
        public event EventHandler<RowChangedEventArgs> RowDeleted;
        public event EventHandler<RowChangedEventArgs> RowUndeleted;
        protected internal void OnRowAdded(RowChangedEventArgs e)
        {
            IsModified = true;
            RowAdded?.Invoke(this, e);
        }
        protected internal void OnRowDeleted(RowChangedEventArgs e)
        {
            IsModified = true;
            RowDeleted?.Invoke(this, e);
        }
        protected internal void OnRowUndeleted(RowChangedEventArgs e)
        {
            IsModified = true;
            RowUndeleted?.Invoke(this, e);
        }

        public Row NewRow()
        {
            Row row = new Row(this);
            DataGridCellInfo cur = Grid.SelectedCells[0];
            int i = Rows.IndexOf(cur.Item);
            if (i == -1)
            {
                i = Rows.Count;
            }
            Rows.Insert(i, row);
            Grid.ItemsSource = null;
            Grid.ItemsSource = Rows;
            //UpdateGrid();
            Grid.CurrentCell = new DataGridCellInfo(row, cur.Column);
            //ScrollViewer sv = App.FindFirstVisualChild<ScrollViewer>(Grid);
            return row;
        }
        public ColumnInfo[] GetForeignKeyColumns(ForeignKeyConstraint constraint)
        {
            if (constraint == null)
            {
                throw new ArgumentNullException("constraint");
            }
            List<ColumnInfo> ret = new List<ColumnInfo>(constraint.Columns.Length);
            foreach (string col in constraint.Columns)
            {
                ret.Add(GetFieldByName(col));
            }
            return ret.ToArray();
        }

        private ColumnInfo[] _fields;
        public ColumnInfo[] Fields
        {
            get
            {
                return _fields;
            }
            private set
            {
                _fields = value;
                //Rows?.Clear();
                //if (Grid != null)
                //{
                //    Grid.Columns.Clear();
                //}
            }
        }
        private string[] _keyFieldNames = new string[0];
        private ColumnInfo[] _keyFields = null;
        private void UpdateKeyFields()
        {
            if (_keyFields != null)
            {
                return;
            }
            if (Fields == null || Fields.Length == 0)
            {
                return;
            }
            List<ColumnInfo> l = new List<ColumnInfo>();
            foreach (string s in _keyFieldNames)
            {
                ColumnInfo fi = GetFieldByName(s);
                if (fi != null)
                {
                    l.Add(fi);
                }
            }
            if (l.Count != _keyFieldNames.Length)
            {
                return;
            }
            _keyFields = l.ToArray();
        }

        public ColumnInfo[] KeyFields
        {
            get
            {
                UpdateKeyFields();
                return _keyFields ?? (new ColumnInfo[0]);
            }
        }
        public RowCollection Rows
        {
            get
            {
                return (RowCollection)GetValue(RowsProperty);
            }
            private set
            {
                SetValue(RowsProperty, value);
            }
        }
        IChangeSetRows IChangeSet.Rows
        {
            get
            {
                return Rows;
            }
        }
        private Table _table;
        public Table Table
        {
            get
            {
                return _table;
            }
            set
            {
                if (ReferenceEquals(_table, value))
                {
                    return;
                }
                _table = value;
                SetKeyFields(_table?.FirstCandidateKey?.Columns);
                //UpdateFieldComment();
            }
        }
        public Db2SourceContext DataSet
        {
            get
            {
                return _table?.Context;
            }
        }
        public bool HasError
        {
            get
            {
                return (bool)GetValue(HasErrorProperty);
            }
            set
            {
                SetValue(HasErrorProperty, value);
            }
        }

        private bool _hasErrorUpdating = false;
        private void UpdateHasError()
        {
            try
            {
                bool flag = false;
                foreach (Row row in Rows)
                {
                    if (row.HasError)
                    {
                        flag = true;
                        break;
                    }
                }
                HasError = flag;
            }
            finally
            {
                _hasErrorUpdating = false;
            }
        }
        public void InvalidateHasError()
        {
            if (_hasErrorUpdating)
            {
                return;
            }
            _hasErrorUpdating = true;
            Dispatcher.Invoke(UpdateHasError, DispatcherPriority.ApplicationIdle);
        }

        private readonly Dictionary<string, ColumnInfo> _nameToField = new Dictionary<string, ColumnInfo>();
        public ColumnInfo GetFieldByName(string name)
        {
            ColumnInfo ret;
            if (_nameToField.TryGetValue(name, out ret))
            {
                return ret;
            }
            return null;
        }
        public void SetKeyFields(string[] keys)
        {
            if (keys == null)
            {
                _keyFieldNames = new string[0];
                _keyFields = null;
                return;
            }
            _keyFieldNames = new string[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                _keyFieldNames[i] = keys[i];
            }
            _keyFields = null;
        }
        public DataGridController()
        {
            Fields = new ColumnInfo[0];
            Rows = new RowCollection(this);
        }

        private void LoadInternal(IDataReader reader)
        {
            if (Grid != null)
            {
                Grid.ItemsSource = null;
            }
            int n = reader.FieldCount;
            Fields = new ColumnInfo[n];
            Rows = new RowCollection(this);
            //DeletedRows = new List<Row>();
            for (int i = 0; i < n; i++)
            {
                ColumnInfo fi = new ColumnInfo(reader, i);
                Column c = Table?.Columns[fi.Name];
                if (c != null)
                {
                    fi.Comment = c.CommentText;
                    fi.StringFormat = c.StringFormat;
                }
                Fields[i] = fi;
                _nameToField[fi.Name] = fi;
            }
            while (reader.Read())
            {
                Rows.Add(new Row(this, reader));
            }
            IsModified = false;
        }
        public void Load(IDataReader reader)
        {
            Table = null;
            try
            {
                LoadInternal(reader);
            }
            finally
            {
                UpdateGrid();
            }
        }
        public void Load(IDataReader reader, Table table)
        {
            Table = table;
            try
            {
                LoadInternal(reader);
                foreach (ColumnInfo info in Fields)
                {
                    Column c = table.Columns[info.Name];
                    if (c == null)
                    {
                        continue;
                    }
                    info.Column = c;
                    info.HiddenLevel = c.HiddenLevel;
                    info.IsNotNull = c.NotNull;
                    if (!string.IsNullOrEmpty(c.DefaultValue))
                    {
                        info.IsDefaultDefined = true;
                        info.DefaultValueExpr = c.DefaultValue;
                    }
                    info.ForeignKeys = table.GetForeignKeysForColumn(info.Name);
                }
            }
            finally
            {
                UpdateGrid();
            }
        }

        public void Save(IDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }
            if (Table == null)
            {
                throw new NotSupportedException("Table is null");
            }
            Row[] log = GetChanges();
            if (log == null || log.Length == 0)
            {
                return;
            }
            foreach (Row row in log)
            {
                row.ClearError();
            }
            Dictionary<IChangeSetRow, bool> applied = new Dictionary<IChangeSetRow, bool>();
            Db2SourceContext ctx = Table.Context;
            IDbTransaction txn = connection.BeginTransaction();
            try
            {
                foreach (Row row in log)
                {
                    ctx.ApplyChange(this, row, connection, txn, applied);
                }
                txn.Commit();
                foreach (Row row in log)
                {
                    row.AcceptChanges();
                }
            }
            catch
            {
                txn.Rollback();
                foreach (Row row in log)
                {
                    if (row.HasError)
                    {
                        Grid.ScrollIntoView(row);
                        Grid.CurrentCell = new DataGridCellInfo(row, Grid.Columns[0]);
                        break;
                    }
                }
                //throw;
            }
            finally
            {
                txn.Dispose();
            }
            Rows.TrimDeletedRows();
            UpdateIsModified();
        }
        public void Save()
        {
            if (Table == null)
            {
                throw new NotSupportedException("Table is null");
            }
            using (IDbConnection conn = Table.Context.NewConnection(true))
            {
                Save(conn);
            }
            Grid.ItemsSource = null;
            Grid.ItemsSource = Rows;
        }

        public void Revert()
        {
            Rows.RevertChanges();
            UpdateIsModified();
            Grid.ItemsSource = null;
            Grid.ItemsSource = Rows;
        }

        public class ColumnInfoConverter: IValueConverter
        {
            public ColumnInfo ColumnInfo { get; set; }
            public ColumnInfoConverter() { }
            internal ColumnInfoConverter(ColumnInfo columnInfo)
            {
                ColumnInfo = columnInfo;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (ColumnInfo == null)
                {
                    return value;
                }
                return ColumnInfo.Convert(value, targetType, parameter, culture);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (ColumnInfo == null)
                {
                    return value;
                }
                return ColumnInfo.ConvertBack(value, targetType, parameter, culture);
            }
        }
        public void UpdateGrid()
        {
            Grid.IsVisibleChanged += Grid_IsVisibleChanged;
            Grid_IsVisibleChanged(Grid, new DependencyPropertyChangedEventArgs(DataGrid.IsVisibleProperty, false, Grid.IsVisible));
            Grid.SelectedCellsChanged += Grid_SelectedCellsChanged;
            bool editable = (Table != null) && (Table.FirstCandidateKey != null) && (Table.FirstCandidateKey.Columns.Length != 0);
            Grid.IsReadOnly = !editable;
            Grid.CanUserAddRows = editable;
            Grid.CanUserDeleteRows = false;

            Grid.Columns.Clear();
            _columnToDataIndex = new Dictionary<DataGridColumn, int>();
            if (editable)
            {
                //DataGridTemplateColumn btn = new DataGridTemplateColumn();
                //btn.CellTemplate = Application.Current.FindResource("DataGridRevertColumnTemplate") as DataTemplate;
                //btn.CellStyle = Application.Current.FindResource("DataGridButtonCellStyle") as Style;
                //btn.HeaderStyle = Application.Current.FindResource("DataGridRevertColumnHeaderStyle") as Style;
                //btn.HeaderTemplate = Application.Current.FindResource("ImageRollback14") as DataTemplate;
                //Grid.Columns.Add(btn);

                DataGridTemplateColumn chk = new DataGridTemplateColumn()
                {
                    CellTemplate = Application.Current.FindResource("DataGridControlColumnTemplate") as DataTemplate,
                    CellStyle = Application.Current.FindResource("DataGridControlCellStyle") as Style,
                    HeaderStyle = Application.Current.FindResource("DataGridControlColumnHeaderStyle") as Style
                };
                Grid.Columns.Add(chk);
            }

            {
                CommandBinding cb;
                cb = new CommandBinding(ApplicationCommands.SelectAll, SelectAllCommand_Executed);
                Grid.CommandBindings.Add(cb);
                cb = new CommandBinding(DataGridCommands.SelectAllCells, SelectAllCells_Executed, CopyTableCommand_CanExecute);
                Grid.CommandBindings.Add(cb);
                cb = new CommandBinding(DataGridCommands.CopyTable, CopyTableCommand_Executed, CopyTableCommand_CanExecute);
                Grid.CommandBindings.Add(cb);
                cb = new CommandBinding(DataGridCommands.CopyTableContent, CopyTableContentCommand_Executed, CopyTableCommand_CanExecute);
                Grid.CommandBindings.Add(cb);
                cb = new CommandBinding(DataGridCommands.CopyTableAsInsert, CopyTableAsInsertCommand_Executed, CopyTableCommand_CanExecute);
                Grid.CommandBindings.Add(cb);
                cb = new CommandBinding(DataGridCommands.CopyTableAsCopy, CopyTableAsCopyCommand_Executed, CopyTableCommand_CanExecute);
                Grid.CommandBindings.Add(cb);
                if (editable)
                {
                    cb = new CommandBinding(DataGridCommands.CheckAll, CheckAllCommand_Executed, CheckAllCommand_CanExecute);
                    Grid.CommandBindings.Add(cb);
                    cb = new CommandBinding(DataGridCommands.UncheckAll, UncheckAllCommand_Executed, UncheckAllCommand_CanExecute);
                    Grid.CommandBindings.Add(cb);
                    cb = new CommandBinding(ApplicationCommands.Paste, PasteCommand_Executed, PasteCommand_CanExecute);
                    Grid.CommandBindings.Add(cb);
                }
            }
            int i = 0;
            foreach (ColumnInfo info in Fields)
            {
                DataGridColumn col;
                Binding b = new Binding(string.Format("[{0}]", i));
                if (info.IsBoolean)
                {
                    DataGridCheckBoxColumn c = new DataGridCheckBoxColumn
                    {
                        Binding = b
                    };
                    col = c;
                }
                else
                {
                    DataGridTextColumn c = new DataGridTextColumn();
                    b.StringFormat = info.StringFormat;
                    b.Converter = new ColumnInfoConverter(info);
                    c.Binding = b;
                    c.ElementStyle = Application.Current.Resources["DataGridTextBlockStyle"] as Style;
                    c.EditingElementStyle = Application.Current.Resources["DataGridTextBoxStyle"] as Style;
                    if (info.IsDateTime && string.IsNullOrEmpty(b.StringFormat))
                    {
                        b.StringFormat = Db2SourceContext.DateTimeFormat;
                    }
                    col = c;
                }
                col.Header = info;
                _columnToDataIndex.Add(col, i);
                Grid.Columns.Add(col);
                i++;
            }
            Grid.ItemsSource = Rows;
        }

        private void Grid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            List<object> lAdd = new List<object>();
            foreach (DataGridCellInfo info in e.AddedCells)
            {
                lAdd.Add(info.Item);
            }
            lAdd.Sort();
            for (int i = lAdd.Count - 1; 0 < i; i--)
            {
                if (lAdd[i] == lAdd[i - 1])
                {
                    lAdd.RemoveAt(i);
                }
            }
            List<object> lDel = new List<object>();
            foreach (DataGridCellInfo info in e.RemovedCells)
            {
                lDel.Add(info.Item);
            }
            lDel.Sort();
            for (int i = lDel.Count - 1; 0 < i; i--)
            {
                if (lDel[i] == lDel[i - 1])
                {
                    lDel.RemoveAt(i);
                }
            }
            for (int i = lDel.Count - 1; 0 <= i; i--)
            {
                int j = lAdd.IndexOf(lDel[i]);
                if (j != -1)
                {
                    lDel.RemoveAt(i);
                    lAdd.RemoveAt(j);
                }
            }
            List<object> l = new List<object>();
            l.AddRange(lAdd);
            l.AddRange(lDel);
            foreach (object item in l)
            {
                foreach (DataGridColumn col in Grid.Columns)
                {
                    DataGridCell cell = App.FindLogicalParent<DataGridCell>(col.GetCellContent(item));
                    if (cell == null)
                    {
                        continue;
                    }
                    CellInfo info = GetCellInfo(cell);
                    info?.UpdateCurrentRow();
                }
            }
        }

        private void Grid_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Grid.IsVisible)
            {
                return;
            }
            if (_searchDataGridTextWindow == null)
            {
                return;
            }
            SearchDataGridControllerWindow win;
            if (_searchDataGridTextWindow.TryGetTarget(out win))
            {
                win.Close();
                _searchDataGridTextWindow = null;
            }
        }

        private void CopyTableCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DataGrid gr = sender as DataGrid;
            e.CanExecute = (gr != null) && (0 < gr.Columns.Count);
            e.Handled = true;
        }
        private static readonly char[] TabTextEscapedChars = new char[] { '\t', '\n', '\r', '"' };
        private static readonly char[] CsvEscapedChars = new char[] { '\n', '\r', '"', ',' };
        //private static readonly char[] HtmlEscapedChars = new char[] { '\n', '\r', ' ', '&', '<', '>' };
        private string EscapedText(string value, char[] EscapedChars)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            if (value.IndexOfAny(EscapedChars) == -1)
            {
                return value;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append('"');
            foreach (char c in value)
            {
                buf.Append(c);
                if (c == '"')
                {
                    buf.Append(c);
                }
            }
            buf.Append('"');
            return buf.ToString();
        }
        //private string ToHtml(string value, bool isHeader)
        //{
        //    StringBuilder buf = new StringBuilder();
        //    buf.Append(isHeader ? "<th>" : "<td>");
        //    int n = value.Length;
        //    for (int i = 0; i < n; i++)
        //    {
        //        char c = value[i];
        //        switch (c)
        //        {
        //            case ' ':
        //                buf.Append("&nbsp;");
        //                break;
        //            case '\n':
        //                buf.Append("<br>");
        //                break;
        //            case '\r':
        //                if (i + 1 < n && value[i + 1] == '\n')
        //                {
        //                    i++;
        //                }
        //                buf.Append("<br>");
        //                break;
        //            case '&':
        //                buf.Append("&amp;");
        //                break;
        //            case '<':
        //                buf.Append("&lt;");
        //                break;
        //            case '>':
        //                buf.Append("&gt;");
        //                break;
        //            default:
        //                buf.Append(c);
        //                if (char.IsSurrogate(c))
        //                {
        //                    i++;
        //                    buf.Append(value[i]);
        //                }
        //                break;
        //        }
        //    }
        //    buf.Append(isHeader ? "</th>" : "</td>");
        //    return buf.ToString();
        //}
        private static int CompareByDisplayIndex(ColumnIndexInfo item1, ColumnIndexInfo item2)
        {
            return item1.Column.DisplayIndex - item2.Column.DisplayIndex;
        }
        private static int CompareDataGridColumnByDisplayIndex(DataGridColumn item1, DataGridColumn item2)
        {
            return item1.DisplayIndex - item2.DisplayIndex;
        }
        private struct ColumnIndexInfo
        {
            public DataGridColumn Column;
            public int DataIndex;
            public string StringFormat;
            internal ColumnIndexInfo(DataGridColumn column, int pos)
            {
                Column = column;
                DataIndex = pos;
                DataGridTextColumn tc = Column as DataGridTextColumn;
                string fmt = tc?.Binding?.StringFormat;
                if (!string.IsNullOrEmpty(fmt))
                {
                    fmt = "{0:" + fmt + "}";
                }
                StringFormat = fmt;
            }
        }
        private struct DataGridInfo
        {
            private ColumnIndexInfo[] _displayColumnMap;
            private int _startRow;
            private int _startColumn;
            private int _endRow;
            private int _endColumn;
            
            public ColumnIndexInfo[] ColumnsByDisplayIndex { get { return _displayColumnMap; } }
            public int StartRow { get { return _startRow; } }
            public int StartColumn { get { return _startColumn; } }
            public int EndRow { get { return _endRow; } }
            public int EndColumn { get { return _endColumn; } }

            public void MoveNext(ref int row, ref int column)
            {
                column++;
                if (column <= EndColumn)
                {
                    return;
                }
                column = StartColumn;
                row++;
                if (row <= EndRow)
                {
                    return;
                }
                row = StartRow;
            }
            public void MovePrevious(ref int row, ref int column)
            {
                column--;
                if (StartColumn <= column)
                {
                    return;
                }
                column = EndColumn;
                row--;
                if (StartRow <= row)
                {
                    return;
                }
                row = EndRow;
            }
            private static ColumnIndexInfo[] GetDisplayColumnsMap(ColumnIndexInfo[] columns)
            {
                int n = 0;
                foreach (ColumnIndexInfo c in columns)
                {
                    n = Math.Max(n, c.Column.DisplayIndex + 1);
                }
                ColumnIndexInfo[] cols = new ColumnIndexInfo[n];
                foreach (ColumnIndexInfo c in columns)
                {
                    cols[c.Column.DisplayIndex] = c;
                }
                return cols;

            }

            public DataGridInfo(DataGridController controller)
            {
                if (controller == null)
                {
                    throw new ArgumentNullException("controller");
                }
                if (controller.Grid == null)
                {
                    throw new ArgumentNullException("controller.Grid");
                }
                DataGrid grid = controller.Grid;
                ColumnIndexInfo[] cols = controller.GetDisplayColumns();
                _displayColumnMap = GetDisplayColumnsMap(cols);
                _startRow = 0;
                for (_endRow = grid.Items.Count - 1; 0 <= _endRow && !(grid.Items[_endRow] is Row); _endRow--) ;
                _startColumn = 0;
                _endColumn = -1;
                if (0 < cols.Length)
                {
                    _startColumn = cols.First().Column.DisplayIndex;
                    _endColumn = cols.Last().Column.DisplayIndex;
                }
            }
        }
        private ColumnIndexInfo[] GetDisplayColumns()
        {
            List<ColumnIndexInfo> l = new List<ColumnIndexInfo>();
            for (int c = 0; c < Grid.Columns.Count; c++)
            {
                DataGridColumn col = Grid.Columns[c];
                int p;
                if (col.Visibility == Visibility.Visible && (_columnToDataIndex.TryGetValue(col, out p) && p != -1))
                {
                    l.Add(new ColumnIndexInfo(col, p));
                }
            }
            l.Sort(CompareByDisplayIndex);
            return l.ToArray();
        }
        public DataGridColumn[] GetDisplayDataGridColumns()
        {
            List<DataGridColumn> cols = new List<DataGridColumn>();
            int c0 = Grid.IsReadOnly ? 1 : 0;
            for (int c = c0; c < Grid.Columns.Count; c++)
            {
                DataGridColumn col = Grid.Columns[c];
                int p;
                if (col.Visibility == Visibility.Visible && (_columnToDataIndex.TryGetValue(col, out p) && p != -1))
                {
                    cols.Add(col);
                }
            }
            cols.Sort(CompareDataGridColumnByDisplayIndex);
            return cols.ToArray();
        }

        private static string GetCellText(object cell, ColumnIndexInfo info)
        {
            if (cell == null)
            {
                return null;
            }
            string s;
            if (string.IsNullOrEmpty(info.StringFormat))
            {
                s = cell.ToString();
            }
            else
            {
                s = string.Format(info.StringFormat, cell);
            }
            return s;
        }

        private delegate bool MatchTextProc(string text, bool ignoreCase);
        private bool _isMatchTextProcValid = false;
        private MatchTextProc _matchTextProc = null;
        private DataGridColumn _matchColumn = null;
        private void UpdateMatchTextProc()
        {
            if (_isMatchTextProcValid)
            {
                return;
            }
            _isMatchTextProcValid = true;
            if (UseRegex)
            {
                if (Wordwrap)
                {
                    _matchTextProc = MatchesRegexWhole;
                }
                else
                {
                    _matchTextProc = MatchesRegex;
                }
            }
            else
            {
                if (Wordwrap)
                {
                    _matchTextProc = EqualsSearchText;
                }
                else
                {
                    _matchTextProc = ContainsSearchText;
                }
            }
            _matchColumn = UseSearchColumn ? SearchColumn : null;
        }
        private void InvalidateMatchTextProc()
        {
            _isMatchTextProcValid = false;
        }

        private bool ContainsSearchText(string text, bool ignoreCase)
        {
            return text.IndexOf(SearchText, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture) != -1;
        }
        private bool EqualsSearchText(string text, bool ignoreCase)
        {
            return string.Equals(text.Trim(), SearchText, ignoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture);
        }

        private bool MatchesRegex(string text, bool ignoreCase)
        {
            Regex re = new Regex(SearchText, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            return re.IsMatch(text);
        }
        private bool MatchesRegexWhole(string text, bool ignoreCase)
        {
            Regex re = new Regex(SearchText, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            Match m = re.Match(text);
            return m != null && m.Index == 0 && m.Length == text.Length;
        }

        /// <summary>
        /// UpdateMatchTextProc()を事前に呼んでいることが前提
        /// </summary>
        /// <param name="text"></param>
        /// <param name="item"></param>
        /// <param name="column"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        private bool MatchesText(string text, object item, DataGridColumn column, object tag)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            if (_matchColumn != null && _matchColumn != column)
            {
                return false;
            }
            return _matchTextProc.Invoke(text, IgnoreCase);
        }

        private WeakReference<SearchDataGridControllerWindow> _searchDataGridTextWindow = null;
        public void ShowSearchWinodow()
        {
            SearchDataGridControllerWindow win = null;
            if (_searchDataGridTextWindow != null)
            {
                _searchDataGridTextWindow.TryGetTarget(out win);
            }
            if (win == null || !win.IsVisible)
            {
                win = new SearchDataGridControllerWindow();
                _searchDataGridTextWindow = new WeakReference<SearchDataGridControllerWindow>(win);
            }
            if (win == null)
            {
                return;
            }
            win.Owner = Window.GetWindow(Grid);
            win.Target = this;
            WindowLocator.LocateNearby(Grid, win, NearbyLocation.UpLeft);
            win.Show();
        }

        /// <summary>
        /// 現在選択されているセルの位置から検索を始める
        /// </summary>
        /// <param name="isFirst">trueの場合現在選択しているセルから検索を開始。falseの場合現在選択しているセルの次のセルから検索を開始</param>
        /// <param name="endRow">検索を終了するセルの行</param>
        /// <param name="endColumn">検索を終了するセルの列</param>
        /// <returns></returns>
        public bool SearchGridTextBackward()
        {
            if (Grid == null)
            {
                return false;
            }
            DataGridInfo gridInfo = new DataGridInfo(this);
            if (gridInfo.EndRow == -1 || gridInfo.EndColumn == -1)
            {
                return false;
            }
            if (string.IsNullOrEmpty(SearchText))
            {
                return false;
            }
            UpdateMatchTextProc();
            if (_matchTextProc == null)
            {
                return false;
            }
            bool isFirst;
            int endRow;
            int endColumn;
            GetSearchEnd(gridInfo, false, out isFirst, out endRow, out endColumn);
            int r0;
            int c0;
            if (!GetCurentCellPosition(false, out r0, out c0))
            {
                r0 = gridInfo.StartRow;
                c0 = gridInfo.StartColumn;
            }
            int r = r0;
            int c = c0;
            if (!isFirst)
            {
                gridInfo.MovePrevious(ref r, ref c);
            }
            do
            {
                Row row = Grid.Items[r] as Row;
                if (row == null)
                {
                    gridInfo.MovePrevious(ref r, ref c);
                    continue;
                }
                ColumnIndexInfo info = gridInfo.ColumnsByDisplayIndex[c];
                object cell = row[info.DataIndex];
                string s = GetCellText(cell, info);
                if (MatchesText(s, row, info.Column, null))
                {
                    Grid.SelectedCells.Clear();
                    Grid.SelectedCells.Add(new DataGridCellInfo(row, info.Column));
                    Grid.ScrollIntoView(row, info.Column);
                    return true;
                }
                gridInfo.MovePrevious(ref r, ref c);
            } while ((r != endRow || c != endColumn) && (r != r0 || c != c0));
            // (r != r0 || c != c0) は本来不要だが無限ループ防止用に念のため
            InvalidateSearchEnd();
            return false;
        }

        /// <summary>
        /// 現在選択されているセルの位置から検索を始める
        /// </summary>
        /// <param name="isFirst">trueの場合現在選択しているセルから検索を開始。falseの場合現在選択しているセルの次のセルから検索を開始</param>
        /// <param name="endRow">検索を終了するセルの行</param>
        /// <param name="endColumn">検索を終了するセルの列</param>
        /// <returns></returns>
        public bool SearchGridTextForward()
        {
            if (Grid == null)
            {
                return false;
            }
            DataGridInfo gridInfo = new DataGridInfo(this);
            if (gridInfo.EndRow == -1 || gridInfo.EndColumn == -1)
            {
                return false;
            }
            if (string.IsNullOrEmpty(SearchText))
            {
                return false;
            }
            UpdateMatchTextProc();
            if (_matchTextProc == null)
            {
                return false;
            }
            bool isFirst;
            int endRow;
            int endColumn;
            GetSearchEnd(gridInfo, true, out isFirst, out endRow, out endColumn);
            int r0;
            int c0;
            if (!GetCurentCellPosition(true, out r0, out c0))
            {
                r0 = gridInfo.EndRow;
                c0 = gridInfo.EndColumn;
            }
            int r = r0;
            int c = c0;
            if (!isFirst)
            {
                gridInfo.MoveNext(ref r, ref c);
            }
            do
            {
                Row row = Grid.Items[r] as Row;
                if (row == null)
                {
                    gridInfo.MoveNext(ref r, ref c);
                    continue;
                }
                ColumnIndexInfo info = gridInfo.ColumnsByDisplayIndex[c];
                object cell = row[info.DataIndex];
                string s = GetCellText(cell, info);
                if (MatchesText(s, row, info.Column, null))
                {
                    Grid.SelectedCells.Clear();
                    Grid.SelectedCells.Add(new DataGridCellInfo(row, info.Column));
                    Grid.ScrollIntoView(row, info.Column);
                    return true;
                }
                gridInfo.MoveNext(ref r, ref c);
            } while ((r != endRow || c != endColumn) && (r != r0 || c != c0));
            // (r != r0 || c != c0) は本来不要だが無限ループ防止用に念のため
            InvalidateSearchEnd();
            return false;
        }

        private string[][] GetCellData(bool includesHeader)
        {
            List<string[]> data = new List<string[]>();
            ColumnIndexInfo[] cols = GetDisplayColumns();
            if (cols.Length == 0)
            {
                return new string[0][];
            }
            if (includesHeader)
            {
                List<string> lH = new List<string>();
                foreach (ColumnIndexInfo info in cols)
                {
                    lH.Add(info.Column?.Header?.ToString());
                }
                data.Add(lH.ToArray());
            }
            foreach (object row in Grid.ItemsSource)
            {
                if (!(row is Row))
                {
                    continue;
                }
                List<string> l = new List<string>();
                foreach (ColumnIndexInfo info in cols)
                {
                    object cell = ((Row)row)[info.DataIndex];
                    string s = GetCellText(cell, info);
                    l.Add(s);
                }
                data.Add(l.ToArray());
            }
            return data.ToArray();
        }

        private void DoCopyTable(bool includesHeader)
        {
            //DataGrid gr = sender as DataGrid;
            string[][] data = GetCellData(includesHeader);
            if (data.Length == 0)
            {
                return;
            }
            StringBuilder tabText = new StringBuilder();
            StringBuilder csvText = new StringBuilder();
            //StringBuilder htmlText = new StringBuilder();
            //htmlText.AppendLine("Version:1.0");
            //htmlText.AppendLine("StartHTML:00000097");
            //htmlText.AppendLine("EndHTML:99999999");
            //htmlText.AppendLine("StartFragment:00000133");
            //htmlText.AppendLine("EndFragment:88888888");
            //htmlText.AppendLine("<html>");
            //htmlText.AppendLine("<body>");
            //htmlText.AppendLine("<!--StartFragment--><table>");

            int nR = data.Length;
            for (int r = 0; r < nR; r++)
            {
                string[] row = data[r];
                int nC = row.Length;
                if (nC == 0)
                {
                    continue;
                }
                tabText.Append(EscapedText(row[0], TabTextEscapedChars));
                csvText.Append(EscapedText(row[0], CsvEscapedChars));
                //htmlText.Append("<tr>");
                //htmlText.Append(ToHtml(row[0], r == 0));
                for (int c = 1; c < nC; c++)
                {
                    tabText.Append('\t');
                    tabText.Append(EscapedText(row[c], TabTextEscapedChars));
                    csvText.Append(',');
                    csvText.Append(EscapedText(row[c], CsvEscapedChars));
                    //htmlText.Append(ToHtml(row[c], r == 0));
                }
                tabText.AppendLine();
                csvText.AppendLine();
                //htmlText.AppendLine("</tr>");
            }
            //htmlText.AppendLine("</table>");
            //htmlText.AppendLine("<!--EndFragment-->");
            //int n2 = Encoding.UTF8.GetByteCount(htmlText.ToString());
            //htmlText.AppendLine("</body>");
            //htmlText.Append("</html>");
            //int n1 = Encoding.UTF8.GetByteCount(htmlText.ToString());
            //htmlText.Replace("99999999", n1.ToString("00000000"), 0, 97);
            //htmlText.Replace("88888888", n2.ToString("00000000"), 0, 97);

            string txt = tabText.ToString();
            string csv = csvText.ToString();
            //string html = htmlText.ToString();
            DataObject obj = new DataObject();
            obj.SetData(DataFormats.Text, txt);
            obj.SetData(DataFormats.UnicodeText, txt);
            //obj.SetData(DataFormats.Html, html);
            obj.SetData(DataFormats.CommaSeparatedValue, csv);
            Clipboard.SetDataObject(obj);
        }

        

        private void SelectAllCells_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Grid.SelectAllCells();
        }
        private void CopyTableCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            DoCopyTable(true);
        }
        private void CopyTableContentCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            DoCopyTable(false);
        }

        private void DoCopyTableAsInsert()
        {
            if (Table == null)
            {
                return;
            }
            Dictionary<object, Dictionary<ColumnInfo, object>> dict = new Dictionary<object, Dictionary<ColumnInfo, object>>();
            switch (Grid.SelectedCells.Count)
            {
                case 0:
                    return;
                case 1:
                    Row row;
                    Dictionary<ColumnInfo, object> values;
                    row = Grid.CurrentItem as Row;
                    if (row == null)
                    {
                        return;
                    }
                    values = new Dictionary<ColumnInfo, object>();
                    dict.Add(row, values);
                    foreach (DataGridColumn col in Grid.Columns)
                    {
                        ColumnInfo c = col.Header as ColumnInfo;
                        if (c == null)
                        {
                            continue;
                        }
                        values.Add(c, row[c.Index]);
                    }
                    break;
                default:
                    foreach (DataGridCellInfo cell in Grid.SelectedCells)
                    {
                        row = cell.Item as Row;
                        if (row == null)
                        {
                            continue;
                        }
                        if (!dict.TryGetValue(row, out values))
                        {
                            values = new Dictionary<ColumnInfo, object>();
                            dict.Add(row, values);
                        }
                        ColumnInfo col = cell.Column.Header as ColumnInfo;
                        if (col == null)
                        {
                            continue;
                        }
                        values.Add(col, row[col.Index]);
                    }
                    break;
            }
            if (dict.Count == 0)
            {
                return;
            }
            StringBuilder buf = new StringBuilder();
            foreach (object row in Grid.ItemsSource)
            {
                Dictionary<ColumnInfo, object> values;
                if (!dict.TryGetValue(row, out values))
                {
                    continue;
                }
                buf.Append(DataSet.GetInsertSql(Table, 0, 80, ";", values));
            }
            DataObject obj = new DataObject();
            obj.SetData(DataFormats.Text, buf.ToString());
            obj.SetData(DataFormats.UnicodeText, buf.ToString());
            Clipboard.SetDataObject(obj);
        }

        private void CopyTableAsInsertCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            DoCopyTableAsInsert();
        }
        private void CopyTableAsCopyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void SelectAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGrid gr = e.Source as DataGrid;
            int c0 = gr.IsReadOnly ? 0 : 1;
            gr.SelectedCells.Clear();
            foreach (object item in gr.Items)
            {
                if (!(item is Row))
                {
                    continue;
                }
                for (int c = c0; c < gr.Columns.Count; c++)
                {
                    DataGridColumn col = gr.Columns[c];
                    gr.SelectedCells.Add(new DataGridCellInfo(item, col));
                }
            }
        }
        private void PasteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DataGrid gr = sender as DataGrid;
            e.CanExecute = (gr != null) && !gr.IsReadOnly;
            e.Handled = true;
        }

        private void PasteAsSingleText(ExecutedRoutedEventArgs e, GridClipboard clipboard)
        {
            if (!Grid.BeginEdit())
            {
                return;
            }
            TextBox tb = Grid.CurrentColumn.GetCellContent(Grid.CurrentItem) as TextBox;
            if (tb == null)
            {
                return;
            }
            tb.Paste();
            e.Handled = true;
        }
        private void PasteAsDataGrid(ExecutedRoutedEventArgs e, GridClipboard clipboard)
        {
            GridClipboardWindow win = new GridClipboardWindow
            {
                Owner = Window.GetWindow(Grid),
                Clipboard = clipboard
            };
            bool? ret = win.ShowDialog();
            if (!ret.HasValue || !ret.Value)
            {
                return;
            }
            clipboard.Paste();
            e.Handled = true;
        }

        private void PasteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGrid gr = e.Source as DataGrid;
            if (gr.IsReadOnly)
            {
                return;
            }
            if (Clipboard.ContainsData(DataFormats.CommaSeparatedValue))
            {
                string csv = Clipboard.GetText(TextDataFormat.CommaSeparatedValue);
                GridClipboard clipboard = new GridClipboard(this, csv, GridClipboard.TextViewFormat.CSV);
                if (clipboard.IsSingleText)
                {
                    PasteAsSingleText(e, clipboard);
                }
                else
                {
                    PasteAsDataGrid(e, clipboard);
                }
            }
        }
        private void CheckAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DataGrid gr = sender as DataGrid;
            e.CanExecute = (gr != null) && !gr.IsReadOnly;
            e.Handled = true;
        }
        private void CheckAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGrid gr = e.Source as DataGrid;
            if (gr == null || gr.IsReadOnly)
            {
                return;
            }
            foreach (object item in gr.Items)
            {
                if (!(item is Row))
                {
                    continue;
                }
                //((Row)item).IsChecked = true;
                ((Row)item).IsDeleted = true;
            }
        }
        private void UncheckAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DataGrid gr = sender as DataGrid;
            e.CanExecute = (gr != null) && !gr.IsReadOnly;
            e.Handled = true;
        }
        private void UncheckAllCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataGrid gr = e.Source as DataGrid;
            if (gr == null || gr.IsReadOnly)
            {
                return;
            }
            foreach (object item in gr.Items)
            {
                if (!(item is Row))
                {
                    continue;
                }
                //((Row)item).IsChecked = false;
                ((Row)item).IsDeleted = false;
            }
        }

        public Row[] GetChanges()
        {
            List<Row> list = new List<Row>();
            foreach (Row r in Rows)
            {
                if (r.ChangeKind != ChangeKind.None)
                {
                    list.Add(r);
                }
            }
            list.AddRange(Rows.TemporaryRows);
            return list.ToArray();
        }
        public void AcceptChanges()
        {
            Rows.AcceptChanges();
        }
    }

    public class DataGridCellToCellInfoConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DataGridCell cell = value as DataGridCell;
            if (cell == null)
            {
                return null;
            }
            CellInfo info = DataGridController.GetCellInfo(cell);
            if (info == null)
            {
                info = new CellInfo()
                {
                    Cell = cell
                };
                DataGridController.SetCellInfo(cell, info);
            }
            return info;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class RowVisibleConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is Row) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HideNewItemPlaceHolderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null || value.GetType() == CollectionView.NewItemPlaceholder.GetType()) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class HasErrorToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Row row = value as Row;
            return (row != null && row.HasError) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //public class ColumnInfoToHorizontalAlignmentConverter: IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        ColumnInfo info = value as ColumnInfo;
    //        if (info == null)
    //        {
    //            return HorizontalAlignment.Left;
    //        }
    //        return info.IsNumeric ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
