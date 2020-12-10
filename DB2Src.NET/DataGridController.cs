using System;
using System.Collections;
using System.Collections.Generic;
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
        public static readonly RoutedCommand CopyTable = InitCopyTable();
        public static readonly RoutedCommand CopyTableContent = InitCopyTableContent();
        public static readonly RoutedCommand CopyTableAsInsert = InitCopyTableAsInsert();
        public static readonly RoutedCommand CopyTableAsCopy = InitCopyTableAsCopy();
        public static readonly RoutedCommand CheckAll = InitCheckAll();
        public static readonly RoutedCommand UncheckAll = InitUncheckAll();

        private static RoutedCommand InitCopyTable()
        {
            RoutedCommand ret = new RoutedCommand("表をコピー", typeof(DataGrid));
            ret.InputGestures.Add(new KeyGesture(Key.T, ModifierKeys.Control | ModifierKeys.Shift));
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
    public class CellInfo: INotifyPropertyChanged
    {
        public Row Row { get; private set; }
        public ColumnInfo Column { get; private set; }
        public int Index { get; private set; }
        public object Data
        {
            get
            {
                return Row[Index];
            }
            set
            {
                if (Row[Index] == value)
                {
                    return;
                }
                Row[Index] = value;
            }
        }
        public object Old
        {
            get
            {
                return Row.Old(Index);
            }
        }
        public bool IsModified
        {
            get
            {
                return Row.IsModified(Index);
            }
        }
        public bool IsFault
        {
            get
            {
                return Column.IsNotNull && (Row[Index] == null);
            }
        }
        public bool IsNullable
        {
            get
            {
                return Column.IsNullable;
            }
        }
        internal CellInfo(Row row, ColumnInfo column)
        {
            Row = row;
            Column = column;
            Index = Column.Index;
        }

        internal void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class CellInfoCollection: IList<CellInfo>
    {
        private List<CellInfo> _list = new List<CellInfo>();

        internal CellInfoCollection(Row owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException("owner");
            }
            ColumnInfo[] cols = owner.Owner.Fields;
            _list = new List<CellInfo>(cols.Length);
            foreach (ColumnInfo col in cols)
            {
                _list.Add(new CellInfo(owner, col));
            }
        }

        #region IListの実装
        public CellInfo this[int index]
        {
            get
            {
                return _list[index];
            }

            set
            {
                _list[index] = value;
            }
        }

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

        public void Add(CellInfo item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(CellInfo item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(CellInfo[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<CellInfo> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(CellInfo item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, CellInfo item)
        {
            _list.Insert(index, item);
        }

        public bool Remove(CellInfo item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        #endregion
    }

    public class Row: IList<object>, IComparable, IChangeSetRow, INotifyPropertyChanged
    {
        private static readonly object Unchanged = new object();
        private DataGridController _owner;
        internal object[] _data;
        internal object[] _old;
        internal bool _added = false;
        internal bool _deleted = false;
        private bool? _hasChanges = null;
        private void UpdateHasChanges()
        {
            if (_added || _deleted)
            {
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
        }
        internal void RevertChanges()
        {
            for (int i = 0; i < _old.Length; i++)
            {
                if (_old[i] != Unchanged)
                {
                    _data[i] = _old[i];
                }
            }
            _hasChanges = false;
            _deleted = false;
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
        public class ModifiedCollection
        {
            private Row _owner;

            public bool this[int index]
            {
                get
                {
                    return _owner.IsModified(index);
                }
            }
            public ModifiedCollection(Row owner)
            {
                _owner = owner;
            }
        }
        public CellInfoCollection Cells { get; private set; }

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
            _hasChanges = null;
            _owner?.OnCellValueChanged(new CellValueChangedEventArgs(this, index));
            OnPropertyChanged(string.Format("[{0}]", index));
            Owner.Rows.OnRowChanged(this);
            OnPropertyChanged("Modified");
            CellInfo cell = Cells[index];
            cell.OnPropertyChanged("Data");
            cell.OnPropertyChanged("IsModified");
            cell.OnPropertyChanged("IsFault");
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
            Cells = new CellInfoCollection(this);
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
            Cells = new CellInfoCollection(this);
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
            Cells = new CellInfoCollection(this);
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
                if (!(o1 is IComparable))
                {
                    return -1;
                }
                int ret = ((IComparable)o1).CompareTo(o2);
                if (ret != 0)
                {
                    return ret;
                }
            }
            return 0;
        }
    }

    public sealed class RowCollection: IList<Row>, IList, IChangeSetRows, INotifyPropertyChanged
    {
        private DataGridController _owner;
        private List<Row> _list = new List<Row>();
        private Dictionary<object[], Row> _keyToRow = null;
        private Dictionary<object[], Row> _oldKeyToRow = new Dictionary<object[], Row>();
        //private List<Row> _deletedRows = new List<Row>();
        private List<Row> _temporaryRows = new List<Row>();

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

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

        public Row FingRowByKey(object[] key)
        {
            RequireKeyToRow();
            Row row;
            if (!_keyToRow.TryGetValue(key, out row))
            {
                return null;
            }
            return row;
        }
        public Row FingRowByOldKey(object[] key)
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
                    _list.Add(r);
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
                    _list.RemoveAt(i);
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
            Row item = _list[index];
            _list.RemoveAt(index);
            InvalidateKeyToRow();
        }

        public void Add(Row item)
        {
            _list.Add(item);
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
            int i = _list.IndexOf(item);
            if (i == -1)
            {
                return _temporaryRows.Remove(item);
            }
            Row row = _list[i];
            _list.RemoveAt(i);
            InvalidateKeyToRow();
            return true;
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
            InvalidateKeyToRow();
            Row row = (Row)value;
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
            InvalidateKeyToRow();
            Row row = (Row)value;
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
        public static readonly DependencyProperty CellInfoProperty = DependencyProperty.RegisterAttached("CellInfo", typeof(CellInfo), typeof(DataGridController));

        public static CellInfo GetCellInfo(DependencyObject obj)
        {
            return (CellInfo)obj.GetValue(CellInfoProperty);
        }
        public static void SetCellInfo(DependencyObject obj, CellInfo value)
        {
            obj.SetValue(CellInfoProperty, value);
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

        private bool _isSearchEndValid = false;
        private int _searchEndRow = -1;
        private int _searchEndColumn = -1;

        private void GetSearchEnd(out bool isFirst, out int endRow, out int endColumn)
        {
            if (_isSearchEndValid)
            {
                isFirst = false;
                endRow = _searchEndRow;
                endColumn = _searchEndColumn;
                return;
            }
            _isSearchEndValid = GetCurentCellPosition(out _searchEndRow, out _searchEndColumn);
            if (_isSearchEndValid)
            {
                _searchEndColumn = GetDataPosition(_searchEndColumn);
            }
            isFirst = true;
            endRow = _searchEndRow;
            endColumn = _searchEndColumn;
        }
        private void InvalidateSearchEnd()
        {
            _isSearchEndValid = false;
        }

        private void UpdateIsModified()
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
                return (_keyFields != null) ? _keyFields : new ColumnInfo[0];
            }
        }
        public RowCollection Rows { get; private set; }
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
        private Dictionary<string, ColumnInfo> _nameToField = new Dictionary<string, ColumnInfo>();
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
                Table.Context.ApplyChange(this, row, connection);
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
            bool editable = (Table != null) && (Table.FirstCandidateKey != null) && (Table.FirstCandidateKey.Columns.Length != 0);
            Grid.IsReadOnly = !editable;
            Grid.CanUserAddRows = editable;
            Grid.CanUserDeleteRows = false;

            Grid.Columns.Clear();
            _columnToDataIndex = new Dictionary<DataGridColumn, int>();
            if (editable)
            {
                DataGridCheckBoxColumn chk = new DataGridCheckBoxColumn();
                chk.Binding = new Binding("IsDeleted");
                Grid.Columns.Add(chk);
                _columnToDataIndex.Add(chk, -1);
                chk.CellStyle = Application.Current.FindResource("DataGridCheckBoxCellStyle") as Style;
                chk.HeaderStyle = Application.Current.FindResource("DataGridCheckBoxColumnHeaderStyle") as Style;
            }

            {
                CommandBinding cb;
                cb = new CommandBinding(ApplicationCommands.SelectAll, SelectAllCommand_Executed);
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
                }
            }
            int i = 0;
            foreach (ColumnInfo info in Fields)
            {
                DataGridColumn col;
                Binding b = new Binding(string.Format("[{0}]", i));
                if (info.IsBoolean)
                {
                    DataGridCheckBoxColumn c = new DataGridCheckBoxColumn();
                    c.Binding = b;
                    col = c;
                }
                else
                {
                    DataGridTextColumn c = new DataGridTextColumn();
                    b.StringFormat = info.StringFormat;
                    b.Converter = new ColumnInfoConverter(info);
                    c.Binding = b;
                    if (info.IsNumeric)
                    {
                        Style style = new Style(typeof(TextBlock));
                        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(2.0, 1.0, 2.0, 1.0)));
                        c.ElementStyle = style;
                        Style editStyle = new Style(typeof(TextBox), c.EditingElementStyle);
                        editStyle.Setters.Add(new Setter(TextBox.TextAlignmentProperty, TextAlignment.Right));
                        c.EditingElementStyle = editStyle;
                    }
                    if (info.IsDateTime)
                    {
                        if (string.IsNullOrEmpty(b.StringFormat))
                        {
                            b.StringFormat = Db2SourceContext.DateTimeFormat;
                        }
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
            SearchDataGridTextWindow win;
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
        private ColumnIndexInfo[] GetColumnsByDisplayIndex()
        {
            List<ColumnIndexInfo> cols = new List<ColumnIndexInfo>();
            for (int c = 0; c < Grid.Columns.Count; c++)
            {
                DataGridColumn col = Grid.Columns[c];
                int p;
                if (col.Visibility == Visibility.Visible && (_columnToDataIndex.TryGetValue(col, out p) && p != -1))
                {
                    cols.Add(new ColumnIndexInfo(col, p));
                }
            }
            cols.Sort(CompareByDisplayIndex);
            return cols.ToArray();
        }
        public DataGridColumn[] GetDataGridColumnsByDisplayIndex()
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
            string s = null;
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

        public DataGridCellInfo? GetSelectedCell()
        {
            IList<DataGridCellInfo> l = Grid.SelectedCells;
            if (0 < l.Count)
            {
                return l[0];
            }
            return null;
        }

        public bool GetCurentCellPosition(out int row, out int column)
        {
            row = -1;
            column = -1;
            if (Grid == null)
            {
                return false;
            }
            DataGridCellInfo? info = GetSelectedCell();
            if (!info.HasValue || !info.Value.IsValid)
            {
                return false;
            }
            row = Grid.Items.IndexOf(info.Value.Item);
            column = info.Value.Column.DisplayIndex;
            return true;
        }

        public int GetDataPosition(int column)
        {
            int cN = Grid.Columns.Count;
            if (column < 0 || cN <= column)
            {
                return -1;
            }
            DataGridColumn col = Grid.Columns[column];
            int pos;
            if (_columnToDataIndex.TryGetValue(col, out pos))
            {
                return pos;
            }
            return -1;
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

        public void MoveFormNearbyGrid(Window window)
        {
            Point p = new Point(Grid.ActualWidth, 0);
            p = Grid.PointToScreen(p);
            WinForm.Screen sc = WinForm.Screen.FromPoint(new System.Drawing.Point((int)p.X, (int)p.Y));
            System.Drawing.Rectangle sr = sc.WorkingArea;
            p = new Point(p.X - window.ActualWidth, p.Y - window.ActualHeight);
            if (p.Y < sr.Top)
            {
                p.Y = sr.Top;
            }
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = p.X;
            window.Top = p.Y;
        }

        private WeakReference<SearchDataGridTextWindow> _searchDataGridTextWindow = null;
        public void ShowSearchWinodow()
        {
            SearchDataGridTextWindow win = null;
            if (_searchDataGridTextWindow != null)
            {
                _searchDataGridTextWindow.TryGetTarget(out win);
            }
            if (win == null || !win.IsVisible)
            {
                win = new SearchDataGridTextWindow();
                _searchDataGridTextWindow = new WeakReference<SearchDataGridTextWindow>(win);
            }
            if (win == null)
            {
                return;
            }
            win.Owner = Window.GetWindow(Grid);
            win.Target = this;
            WindowLocator.LocateNearby(Grid, win, NearbyLocation.UpRight);
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
            ColumnIndexInfo[] cols = GetColumnsByDisplayIndex();
            if (cols.Length == 0)
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
            GetSearchEnd(out isFirst, out endRow, out endColumn);
            DataGridCellInfo sel = Grid.SelectedCells.First();
            int r0 = Grid.Items.IndexOf(sel.Item);
            int c0;
            if (_columnToDataIndex.TryGetValue(sel.Column, out c0))
            {
                if (!isFirst)
                {
                    c0--;
                }
            }
            if (r0 < endRow || (endRow == r0 && c0 <= endColumn))
            {
                for (int r = r0; 0 <= r; r--)
                {
                    object row = Grid.Items[r];
                    if (!(row is Row))
                    {
                        continue;
                    }
                    for (int c = c0; 0 <= c; c--)
                    {
                        ColumnIndexInfo info = cols[c];
                        object cell = ((Row)row)[info.DataIndex];
                        string s = GetCellText(cell, info);
                        if (MatchesText(s, row, info.Column, null))
                        {
                            Grid.SelectedCells.Clear();
                            Grid.SelectedCells.Add(new DataGridCellInfo(row, info.Column));
                            Grid.ScrollIntoView(row, info.Column);
                            return true;
                        }
                    }
                    c0 = cols.Length - 1;
                }
                r0 = Grid.Items.Count - 1;
            }
            for (int r = r0; endRow <= r; r--)
            {
                object row = Grid.Items[r];
                if (!(row is Row))
                {
                    continue;
                }
                int cN = (r == endRow) ? endColumn + 1 : 0;
                for (int c = c0; cN <= c; c--)
                {
                    ColumnIndexInfo info = cols[c];
                    object cell = ((Row)row)[info.DataIndex];
                    string s = GetCellText(cell, info);
                    if (MatchesText(s, row, info.Column, null))
                    {
                        Grid.SelectedCells.Clear();
                        Grid.SelectedCells.Add(new DataGridCellInfo(row, info.Column));
                        Grid.ScrollIntoView(row, info.Column);
                        return true;
                    }
                }
                c0 = cols.Length - 1;
            }
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
            ColumnIndexInfo[] cols = GetColumnsByDisplayIndex();
            if (cols.Length == 0)
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
            GetSearchEnd(out isFirst, out endRow, out endColumn);
            DataGridCellInfo sel = Grid.SelectedCells.First();
            int r0 = Grid.Items.IndexOf(sel.Item);
            int c0;
            if (_columnToDataIndex.TryGetValue(sel.Column, out c0))
            {
                if (!isFirst)
                {
                    c0++;
                }
            }
            if (endRow < r0 || (endRow == r0 && endColumn <= c0))
            {
                for (int r = r0; r < Grid.Items.Count; r++)
                {
                    object row = Grid.Items[r];
                    if (!(row is Row))
                    {
                        continue;
                    }
                    for (int c = c0; c < cols.Length; c++)
                    {
                        ColumnIndexInfo info = cols[c];
                        object cell = ((Row)row)[info.DataIndex];
                        string s = GetCellText(cell, info);
                        if (MatchesText(s, row, info.Column, null))
                        {
                            Grid.SelectedCells.Clear();
                            Grid.SelectedCells.Add(new DataGridCellInfo(row, info.Column));
                            Grid.ScrollIntoView(row, info.Column);
                            return true;
                        }
                    }
                    c0 = 0;
                }
                r0 = 0;
            }
            for (int r = r0; r <= endRow; r++)
            {
                object row = Grid.Items[r];
                if (!(row is Row))
                {
                    continue;
                }
                int cN = (r == endRow) ? endColumn : cols.Length;
                for (int c = c0; c < cN; c++)
                {
                    ColumnIndexInfo info = cols[c];
                    object cell = ((Row)row)[info.DataIndex];
                    string s = GetCellText(cell, info);
                    if (MatchesText(s, row, info.Column, null))
                    {
                        Grid.SelectedCells.Clear();
                        Grid.SelectedCells.Add(new DataGridCellInfo(row, info.Column));
                        Grid.ScrollIntoView(row, info.Column);
                        return true;
                    }
                }
                c0 = 0;
            }
            InvalidateSearchEnd();
            return false;
        }

        private string[][] GetCellData(bool includesHeader)
        {
            List<string[]> data = new List<string[]>();
            ColumnIndexInfo[] cols = GetColumnsByDisplayIndex();
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
        private void CopyTableAsInsertCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            if (Table == null)
            {
                return;
            }
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
        private string[][] CsvToArray(string csv)
        {
            if (string.IsNullOrEmpty(csv))
            {
                return new string[0][];
            }
            int i0 = 0;
            int n = csv.Length;
            List<string[]> rows = new List<string[]>();
            List<string> cols = new List<string>();
            for (int i = 0; i < n; i++)
            {
                char c = csv[i];
                switch (c)
                {
                    case '"':
                        for (i++; i < n && csv[i] != '"'; i++) ;
                        break;
                    case '\n':
                        rows.Add(cols.ToArray());
                        cols = new List<string>();
                        i0 = i + 1;
                        break;
                    case '\r':
                        if (csv[i + 1] == '\n')
                        {
                            i++;
                        }
                        rows.Add(cols.ToArray());
                        cols = new List<string>();
                        i0 = i + 1;
                        break;
                    case ',':
                        //cols.Add(DequoteStr(csv.Substring(i0, i - i0)));
                        break;
                }
                //for (; csv[i] != '\r' && csv)
            }
            return new string[0][];
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
                return ColumnInfo.Stub;
            }
            ColumnInfo col = cell.Column.Header as ColumnInfo;
            if (col == null || col.Index == -1)
            {
                return ColumnInfo.Stub;
            }
            Row row = cell.DataContext as Row;
            if (row == null)
            {
                return ColumnInfo.Stub;
            }
            return row.Cells[col.Index];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
