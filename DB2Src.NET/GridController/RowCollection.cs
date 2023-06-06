using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Db2Source
{
    public sealed class RowCollection : ObservableCollection<Row>, IChangeSetRows
    {
        public DataGridController Owner { get; }
        private Dictionary<DataArray, Row> _keyToRow = null;
        private readonly Dictionary<DataArray, Row> _oldKeyToRow = new Dictionary<DataArray, Row>();

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            InvalidateKeyToRow();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Row row in e.NewItems)
                    {
                        if (row.ChangeKind != ChangeKind.New)
                        {
                            _oldKeyToRow[row.GetOldKeys()] = row;
                        }
                        row.BecomeUndeleted();
                        Owner?.OnRowAdded(new RowChangedEventArgs(row));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Row row in e.OldItems)
                    {
                        if (row.ChangeKind != ChangeKind.New)
                        {
                            _oldKeyToRow.Remove(row.GetOldKeys());
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _oldKeyToRow.Clear();
                    foreach (Row row in Items)
                    {
                        if (row.ChangeKind != ChangeKind.New)
                        {
                            _oldKeyToRow[row.GetOldKeys()] = row;
                        }
                    }
                    break;
            }
        }

        private void RequireKeyToRow()
        {
            if (_keyToRow != null)
            {
                return;
            }
            _keyToRow = new Dictionary<DataArray, Row>();
            foreach (Row row in Items)
            {
                _keyToRow[row.GetKeys()] = row;
            }
        }
        internal void InvalidateKeyToRow()
        {
            _keyToRow = null;
        }

        public Row FindRowByKey(DataArray key)
        {
            RequireKeyToRow();
            Row row;
            if (!_keyToRow.TryGetValue(key, out row))
            {
                return null;
            }
            return row;
        }
        public Row FindRowByOldKey(DataArray key)
        {
            Row row;
            if (!_oldKeyToRow.TryGetValue(key, out row))
            {
                return null;
            }
            return row;
        }
        IChangeSetRow IChangeSetRows.FindRowByOldKey(DataArray key)
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
            // AcceptChanges()内でItemsの内容を削除する可能性があるためItemsを逆順サーチ
            for (int i = Items.Count - 1; 0 <= i; i--)
            {
                Row r = Items[i];
                r.AcceptChanges();
            }
        }

        public void RevertChanges()
        {
            for (int i = Items.Count - 1; 0 <= i; i--)
            {
                Row item = Items[i];
                switch (item.ChangeKind)
                {
                    case ChangeKind.New:
                        Items.RemoveAt(i);
                        break;
                    case ChangeKind.Modify:
                    case ChangeKind.Delete:
                        item.RevertChanges();
                        break;
                }
            }
        }

        internal void TrimDeletedRows()
        {
            for (int i = Items.Count - 1; 0 <= i; i--)
            {
                Row row = Items[i];
                if (row._added && row._deleted)
                {
                    RemoveAt(i);
                }
            }
        }

        internal RowCollection(DataGridController owner)
        {
            Owner = owner;
        }

        internal void OnRowChanged(Row row)
        {
            int i = IndexOf(row);
            if (i == -1)
            {
                return;
            }
            OnPropertyChanged(new PropertyChangedEventArgs(string.Format("[{0}]", i)));
        }
    }
}
