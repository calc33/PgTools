using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Db2Source
{
    [Flags]
    public enum TriggerEvent
    {
        Unknown = 1,
        Insert = 2,
        Delete = 4,
        Truncate = 8,
        Update = 16,
    }

    public enum TriggerOrientation
    {
        Unknown,
        Statement,
        Row
    }
    
    public enum TriggerTiming
    {
        Unknown,
        Before,
        After,
        InsteadOf
    }

    public partial class Trigger: SchemaObject
    {
        public class StringCollection: IList<string>, IList
        {
            private readonly Trigger _owner;
            private readonly List<string> _items = new List<string>();

            public void AddRange(IEnumerable<string> collection)
            {
                _items.AddRange(collection);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.AddRange, -1, null, null));
            }

            #region interfaceの実装
            public string this[int index]
            {
                get
                {
                    return ((IList<string>)_items)[index];
                }

                set
                {
                    string sNew = value;
                    string sOld = _items[index];
                    ((IList<string>)_items)[index] = value;
                    OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Update, index, sNew, sOld));
                }
            }

            object IList.this[int index]
            {
                get
                {
                    return ((IList)_items)[index];
                }

                set
                {
                    string sNew = (string)value;
                    string sOld = _items[index];
                    ((IList)_items)[index] = value;
                    OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Update, index, sNew, sOld));
                }
            }

            public int Count
            {
                get
                {
                    return _items.Count;
                }
            }

            public bool IsFixedSize
            {
                get
                {
                    return false;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public bool IsSynchronized
            {
                get
                {
                    return ((IList)_items).IsSynchronized;
                }
            }

            public object SyncRoot
            {
                get
                {
                    return ((IList)_items).SyncRoot;
                }
            }

            public int Add(object value)
            {
                int ret = ((IList)_items).Add(value);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Add, ret, (string)value, null));
                return ret;
            }

            public void Add(string item)
            {
                int ret = ((IList)_items).Add(item);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Add, ret, item, null));
            }

            public void Clear()
            {
                if (_items.Count == 0)
                {
                    return;
                }
                _items.Clear();
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Clear, -1, null, null));
            }

            public bool Contains(object value)
            {
                return _items.Contains((string)value);
            }

            public bool Contains(string item)
            {
                return _items.Contains(item);
            }

            public void CopyTo(Array array, int index)
            {
                ((IList)_items).CopyTo(array, index);
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                ((IList<string>)_items).CopyTo(array, arrayIndex);
            }

            public IEnumerator<string> GetEnumerator()
            {
                return ((IList<string>)_items).GetEnumerator();
            }

            public int IndexOf(object value)
            {
                return ((IList)_items).IndexOf(value);
            }

            public int IndexOf(string item)
            {
                return ((IList<string>)_items).IndexOf(item);
            }

            public void Insert(int index, object value)
            {
                ((IList)_items).Insert(index, value);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Add, index, (string)value, null));
            }

            public void Insert(int index, string item)
            {
                ((IList<string>)_items).Insert(index, item);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Add, index, item, null));
            }

            public void Remove(object value)
            {
                if (_items.Remove((string)value))
                {
                    OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Remove, -1, null, (string)value));
                }
            }

            public bool Remove(string item)
            {
                bool ret = _items.Remove(item);
                if (ret)
                {
                    OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Remove, -1, null, item));
                }
                return ret;
            }

            public void RemoveAt(int index)
            {
                string sOld = _items[index];
                _items.RemoveAt(index);
                OnUpdateColumnChanged(new CollectionOperationEventArgs<string>("UpdateEventColumns", CollectionOperation.Remove, index, null, sOld));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IList<string>)_items).GetEnumerator();
            }
            #endregion
            private void OnUpdateColumnChanged(CollectionOperationEventArgs<string> e)
            {
                _owner?.OnUpdateColumnChanged(e);
            }
            internal StringCollection(Trigger owner)
            {
                _owner = owner;
            }
            public override bool Equals(object obj)
            {
                if (!(obj is StringCollection))
                {
                    return false;
                }
                List<string> l1 = new List<string>(_items);
                List<string> l2 = new List<string>((StringCollection)obj);
                if (l1.Count != l2.Count)
                {
                    return false;
                }
                l1.Sort();
                l2.Sort();
                for (int i = 0; i < l1.Count; i++)
                {
                    if (l1[i] != l2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            public override int GetHashCode()
            {
                int ret = 0;
                foreach (string s in _items)
                {
                    ret = ret * 17 + s.GetHashCode();
                }
                return ret;
            }
            public override string ToString()
            {
                if (Count == 0)
                {
                    return "[]";
                }
                StringBuilder buf = new StringBuilder();
                buf.Append('[');
                buf.Append(_items[0]);
                for (int i = 1; i < _items.Count; i++)
                {
                    buf.Append(", ");
                    buf.Append(_items[i]);
                }
                buf.Append(']');
                return buf.ToString();
            }
        }
        public override string GetSqlType()
        {
            return "TRIGGER";
        }
        public override string GetExportFolderName()
        {
            return "Trigger";
        }

        protected override string GetIdentifier()
        {
            return base.GetIdentifier() + "@" + Table.Identifier;
        }
        private TriggerTiming _timing;
        private string _timingText;
        private TriggerEvent _event;
        private string _eventText;
        private StringCollection _updateEventColumns;
        private string _tableSchema;
        private string _tableName;
        private SchemaObject _table;
        private string _referencedTableName;
        private string _procedureSchema;
        private string _procedureName;
        private StoredFunction _procedure;
        private TriggerOrientation _orientation;
        private string _orientationText;
        private string _referenceNewTable;
        private string _referenceOldTable;
        private string _condition;
        private string _referenceNewRow;
        private string _referenceOldRow;
        private string _definition;
        private Trigger _backup;
        //private string _oldDefinition;

        public TriggerTiming Timing
        {
            get
            {
                return _timing;
            }
            set
            {
                if (_timing == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Timing", value, _timing);
                _timing = value;
                OnPropertyChanged(e);
            }
        }
        public string TimingText
        {
            get
            {
                return _timingText;
            }
            set
            {
                if (_timingText == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("TimingText", value, _timingText);
                _timingText = value;
                OnPropertyChanged(e);
            }
        }
        public TriggerEvent Event
        {
            get
            {
                return _event;
            }
            set
            {
                if (_event == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Event", value, _event);
                _event = value;
                OnPropertyChanged(e);
            }
        }
        public string EventText
        {
            get
            {
                return _eventText;
            }
            set
            {
                if (_eventText == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("EventText", value, _eventText);
                _eventText = value;
                OnPropertyChanged(e);
            }
        }

        public StringCollection UpdateEventColumns
        {
            get
            {
                return _updateEventColumns;
            }
        }

        public string UpdateEventColumnsText
        {
            get
            {
                StringBuilder buf = new StringBuilder();
                bool needComma = false;
                foreach (string s in UpdateEventColumns)
                {
                    if (needComma)
                    {
                        buf.Append(", ");
                    }
                    buf.Append(s);
                    needComma = true;
                }
                return buf.ToString();
            }
            set
            {
                string[] cols = value.Split(',');
                for (int i = 0; i < cols.Length; i++)
                {
                    cols[i] = cols[i].Trim();
                }
                _updateEventColumns.Clear();
                _updateEventColumns.AddRange(cols);
            }
        }

        public bool HasUpdateEventColumns()
        {
            return (0 < _updateEventColumns.Count);
        }
        public string GetUpdateEventColumnsSql(string prefix, string indent, ref int pos, ref int line, int softLimit, int hardLimit)
        {
            if (indent == null)
            {
                throw new ArgumentNullException("indent");
            }
            if (UpdateEventColumns.Count == 0)
            {
                return string.Empty;
            }
            StringBuilder buf = new StringBuilder(prefix);
            int c = pos + (prefix != null ? prefix.Length : 0);
            bool needComma = false;
            foreach (string s in UpdateEventColumns)
            {
                if (needComma)
                {
                    buf.Append(',');
                    c++;
                    if (c < softLimit)
                    {
                        buf.Append(' ');
                        c++;
                    }
                    else
                    {
                        buf.AppendLine();
                        line++;
                        buf.Append(indent);
                        c = indent.Length;
                    }
                }
                if (hardLimit <= c + s.Length)
                {
                    buf.AppendLine();
                    line++;
                    buf.Append(indent);
                    c = indent.Length;
                }
                buf.Append(s);
                c += s.Length;
                needComma = true;
            }
            pos = c;
            return buf.ToString();
        }

        private void UpdateTable()
        {
            if (_table != null)
            {
                return;
            }
            _table = Context?.Objects[TableSchema, TableName];
        }
        public void InvalidateTable()
        {
            _table = null;
        }
        public SchemaObject Table
        {
            get
            {
                UpdateTable();
                return _table;
            }
            set
            {
                if (_table == value)
                {
                    return;
                }
                _table = value;
                if (_table == null)
                {
                    return;
                }
                PropertyChangedEventArgs e1 = null;
                PropertyChangedEventArgs e2 = null;
                if (_tableSchema != _table.SchemaName)
                {
                    e1 = new PropertyChangedEventArgs("TableSchema", _table.SchemaName, _tableSchema);
                    _tableSchema = _table.SchemaName;
                }
                if (_tableName != _table.Name)
                {
                    e2 = new PropertyChangedEventArgs("TableName", _table.Name, _tableName);
                    _tableSchema = _table.SchemaName;
                }
                if (e1 != null)
                {
                    OnPropertyChanged(e1);
                }
                if (e2 != null)
                {
                    OnPropertyChanged(e2);
                }
            }
        }
        public string TableSchema
        {
            get
            {
                return _tableSchema;
            }
            set
            {
                if (_tableSchema == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("TableSchema", value, _tableSchema);
                _tableSchema = value;
                InvalidateTable();
                OnPropertyChanged(e);
            }
        }
        public string TableName
        {
            get
            {
                return _tableName;
            }
            set
            {
                if (_tableName == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("TableName", value, _tableName);
                _tableName = value;
                InvalidateTable();
                OnPropertyChanged(e);
            }
        }

        private void InvalidateProcedure()
        {
            _procedure = null;
        }
        private void UpdateProcedure()
        {
            if (_procedure != null)
            {
                return;
            }
            _procedure = Context?.StoredFunctions[_procedureSchema, _procedureName];
        }
        public StoredFunction Procedure
        {
            get
            {
                UpdateProcedure();
                return _procedure;
            }
        }
        public string ProcedureSchema
        {
            get { return _procedureSchema; }
            set
            {
                if (_procedureSchema == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ProcedureSchema", value, _procedureSchema);
                _procedureSchema = value;
                InvalidateProcedure();
                OnPropertyChanged(e);
            }
        }
        public string ProcedureName
        {
            get { return _procedureName; }
            set
            {
                if (_procedureName == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ProcedureName", value, _procedureName);
                _procedureName = value;
                InvalidateProcedure();
                OnPropertyChanged(e);
            }
        }

        public string ReferencedTableName
        {
            get
            {
                return _referencedTableName;
            }
            set
            {
                if (_referencedTableName == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ReferencedTableName", value, _referencedTableName);
                _referencedTableName = value;
                OnPropertyChanged(e);
            }
        }
        public TriggerOrientation Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                if (_orientation == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Orientation", value, _orientation);
                _orientation = value;
                OnPropertyChanged(e);
            }
        }

        public string OrientationText
        {
            get
            {
                return _orientationText;
            }
            set
            {
                if (_orientationText == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("OrientationText", value, _orientationText);
                _orientationText = value;
                OnPropertyChanged(e);
            }
        }
        public string ReferenceNewTable
        {
            get
            {
                return _referenceNewTable;
            }
            set
            {
                if (_referenceNewTable == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ReferenceNewTable", value, _referenceNewTable);
                _referenceNewTable = value;
                OnPropertyChanged(e);
            }
        }
        public string ReferenceOldTable
        {
            get
            {
                return _referenceOldTable;
            }
            set
            {
                if (_referenceOldTable == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ReferenceOldTable", value, _referenceOldTable);
                _referenceOldTable = value;
                OnPropertyChanged(e);
            }
        }
        public string Condition
        {
            get
            {
                return _condition;
            }
            set
            {
                if (_condition == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Condition", value, _condition);
                _condition = value;
                OnPropertyChanged(e);
            }
        }

        public string ReferenceNewRow
        {
            get
            {
                return _referenceNewRow;
            }
            set
            {
                if (_referenceNewRow == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ReferenceNewRow", value, _referenceNewRow);
                _referenceNewRow = value;
                OnPropertyChanged(e);
            }
        }
        public string ReferenceOldRow
        {
            get
            {
                return _referenceOldRow;
            }
            set
            {
                if (_referenceOldRow == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("ReferenceOldRow", value, _referenceOldRow);
                _referenceOldRow = value;
                OnPropertyChanged(e);
            }
        }

        public string Definition
        {
            get
            {
                return _definition;
            }
            set
            {
                if (_definition == value)
                {
                    return;
                }
                PropertyChangedEventArgs e = new PropertyChangedEventArgs("Definition", value, _definition);
                _definition = value;
                OnPropertyChanged(e);
            }
        }

        public override void Backup()
        {
            _backup = new Trigger(this);
        }

        protected void RestoreFrom(Trigger backup)
        {
            base.RestoreFrom(backup);
            UpdateEventColumns.Clear();
            UpdateEventColumns.AddRange(backup.UpdateEventColumns);
            TableSchema = backup.TableSchema;
            TableName = backup.TableName;
            Definition = backup.Definition;
        }
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            RestoreFrom(_backup);
        }

        public override bool ContentEquals(NamedObject obj)
        {
            Trigger t = obj as Trigger;
            return base.ContentEquals(obj)
                && UpdateEventColumns.Equals(t.UpdateEventColumns)
                && (TableSchema == t.TableSchema)
                && (TableName == t.TableName)
                && (Definition == t.Definition);
        }

        public override Schema.CollectionIndex GetCollectionIndex()
        {
            return Schema.CollectionIndex.Triggers;
        }
        public string[] ExtraInfo { get; set; }
        internal Trigger(Db2SourceContext context, string owner, string triggerSchema, string triggerName, string tableSchema, string tableName, string defintion, bool isLoaded) : base(context, owner, triggerSchema, triggerName, Schema.CollectionIndex.Triggers)
        {
            _updateEventColumns = new StringCollection(this);
            _tableSchema = tableSchema;
            _tableName = tableName;
            _definition = defintion;
            //if (isLoaded)
            //{
            //    _oldDefinition = _definition;
            //}
        }
        public Trigger(Trigger basedOn): base(basedOn)
        {
            _updateEventColumns = new StringCollection(this);
            _updateEventColumns.AddRange(basedOn.UpdateEventColumns);
            _tableSchema = basedOn.TableSchema;
            _tableName = basedOn.TableName;
            _definition = basedOn.Definition;
            //_oldDefinition = _definition;
        }

        public string[] GetAlterSQL(string prefix, string postfix, int indent, bool addNewline)
        {
            if (_backup == null)
            {
                return new string[0];
            }
            return Context.GetAlterSQL(this, _backup, prefix, postfix, indent, addNewline);
        }

        /// <summary>
        /// GetAlterSQLで返したSQLを実行して失敗した場合に元に戻すSQLを返す
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="postfix"></param>
        /// <param name="indent"></param>
        /// <param name="addNewline"></param>
        /// <returns></returns>
        public string[] GetRecoverSQL(string prefix, string postfix, int indent, bool addNewline)
        {
            if (_backup == null)
            {
                return new string[0];
            }
            string s = Context.GetSQL(_backup, string.Empty, string.Empty, 0, false);
            if (string.IsNullOrEmpty(s))
            {
                return new string[0];
            }
            return new string[] { s };
        }
    }

    public sealed class TriggerCollection : IList<Trigger>, IList
    {
        private SchemaObject _owner;
        private List<Trigger> _list = null;
        private Dictionary<string, Trigger> _nameToTrigger = null;

        public TriggerCollection(SchemaObject owner)
        {
            _owner = owner;
        }
        internal TriggerCollection(SchemaObject owner, TriggerCollection basedOn)
        {
            _owner = owner;
            foreach (Trigger t in basedOn)
            {
                Add(new Trigger(t));
            }
        }

        public void Invalidate()
        {
            _list = null;
            _nameToTrigger = null;
        }

        private void RequireItems()
        {
            if (_list != null)
            {
                return;
            }
            _list = new List<Trigger>();
            if (_owner == null || _owner.Schema == null)
            {
                return;
            }
            foreach (Trigger t in _owner.Schema.Triggers)
            {
                if (t == null)
                {
                    continue;
                }
                if (t.Table == null)
                {
                    continue;
                }
                if (!t.Table.Equals(_owner))
                {
                    continue;
                }
                _list.Add(t);
            }
            _list.Sort();
        }
        private void RequireNameToTrigger()
        {
            if (_nameToTrigger != null)
            {
                return;
            }
            _nameToTrigger = new Dictionary<string, Trigger>();
            RequireItems();
            foreach (Trigger c in _list)
            {
                _nameToTrigger.Add(c.Name, c);
            }
        }
        public Trigger this[int index]
        {
            get
            {
                RequireItems();
                return _list[index];
            }
        }
        public Trigger this[string name]
        {
            get
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }
                RequireNameToTrigger();
                Trigger ret;
                if (!_nameToTrigger.TryGetValue(name, out ret))
                {
                    return null;
                }
                return ret;
            }
        }
        internal bool TriggerNameChanging(Trigger Trigger, string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                return true;
            }
            if (Trigger != null && Trigger.Name == newName)
            {
                return true;
            }
            return (this[newName] == null);
        }
        internal void TriggerNameChanged(Trigger Trigger)
        {
            _nameToTrigger = null;
        }
        #region ICollection<Trigger>の実装
        public int Count
        {
            get
            {
                RequireItems();
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

        bool IList.IsFixedSize
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
                RequireItems();
                return ((IList)_list)[index];
            }

            set
            {
                RequireItems();
                ((IList)_list)[index] = value;
            }
        }

        Trigger IList<Trigger>.this[int index]
        {
            get
            {
                RequireItems();
                return _list[index];
            }

            set
            {
                RequireItems();
                _list[index] = value;
            }
        }

        public void Add(Trigger item)
        {
            RequireItems();
            _list.Add(item);
            _nameToTrigger = null;
        }
        int IList.Add(object value)
        {
            Trigger item = value as Trigger;
            RequireItems();
            int ret = -1;
            ret = ((IList)_list).Add(item);
            _nameToTrigger = null;
            return ret;
        }

        public void Clear()
        {
            _list?.Clear();
            _nameToTrigger = null;
        }

        public bool Contains(Trigger item)
        {
            RequireItems();
            return _list.Contains(item);
        }

        bool IList.Contains(object value)
        {
            RequireItems();
            return ((IList)_list).Contains(value);
        }

        public void CopyTo(Trigger[] array, int arrayIndex)
        {
            RequireItems();
            _list.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            RequireItems();
            ((IList)_list).CopyTo(array, index);
        }

        public IEnumerator<Trigger> GetEnumerator()
        {
            RequireItems();
            return _list.GetEnumerator();
        }

        public bool Remove(Trigger item)
        {
            RequireItems();
            bool ret = _list.Remove(item);
            if (ret)
            {
                _nameToTrigger = null;
            }
            return ret;
        }

        void IList.Remove(object value)
        {
            RequireItems();
            bool ret = _list.Remove(value as Trigger);
            if (ret)
            {
                _nameToTrigger = null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            RequireItems();
            return _list.GetEnumerator();
        }

        public int IndexOf(Trigger item)
        {
            return _list.IndexOf(item);
        }

        int IList.IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public void Insert(int index, Trigger item)
        {
            _list.Insert(index, item);
        }
        void IList.Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
        }


        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }
        #endregion
    }
}
