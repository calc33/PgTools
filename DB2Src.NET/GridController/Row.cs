using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;

namespace Db2Source
{
    public class Row : IList<object>, IComparable, IChangeSetRow, INotifyPropertyChanged
    {
        private static readonly object Unchanged = new object();
        private readonly DataGridController _owner;
        internal DataArray _data;
        internal DataArray _old;
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
        //public bool IsChecked { get; set; }
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

        public DataArray GetKeys()
        {
            if (_owner == null || _owner.KeyFields == null || _owner.KeyFields.Length == 0)
            {
                return _data;
            }
            DataArray ret = new DataArray(_owner.KeyFields.Length);
            for (int i = 0; i < _owner.KeyFields.Length; i++)
            {
                ColumnInfo f = _owner.KeyFields[i];
                ret[i] = _data[f.Index];
            }
            return ret;
        }
        public DataArray GetOldKeys()
        {
            if (_owner == null || _owner.KeyFields == null || _owner.KeyFields.Length == 0)
            {
                return _data;
            }
            DataArray ret = new DataArray(_owner.KeyFields.Length);
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
                return _data.Length;
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

        public Row(DataGridController owner, IDataReader reader)
        {
            _owner = owner;
            _added = false;
            _deleted = false;
            _data = new DataArray(reader.FieldCount);
            _old = new DataArray(reader.FieldCount);
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
            _data = new DataArray(owner.Fields.Length);
            _old = new DataArray(owner.Fields.Length);
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
            _data = new DataArray(0);
            _old = new DataArray(0);
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
            DataArray k1 = GetOldKeys();
            DataArray k2 = ((Row)obj).GetOldKeys();
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
            DataArray k = GetOldKeys();
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
        public static int CompareKey(DataArray item1, DataArray item2)
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
}
