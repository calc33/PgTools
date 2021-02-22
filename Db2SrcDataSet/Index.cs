namespace Db2Source
{
    public class Index: SchemaObject
    {
        private Table _table;
        private string _tableSchema;
        private string _tableName;
        //private string[] _columns;

        public bool IsUnique { get; set; }
        public bool IsImplicit { get; set; }
        private void UpdateTable()
        {
            if (_table != null)
            {
                return;
            }
            _table = Context?.Tables[TableSchema, TableName];
        }
        public void InvalidateTable()
        {
            _table = null;
        }
        public Table Table
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
        public string IndexType { get; set; }
        public string[] Columns { get; set; }
        //private string _definition;
        public override string GetSqlType()
        {
            return "INDEX";
        }
        public override string GetExportFolderName()
        {
            return "Index";
        }
        public override Schema.CollectionIndex GetCollectionIndex()
        {
            return Schema.CollectionIndex.Indexes;
        }

        internal Index _backup;
        public override void Backup()
        {
            _backup = new Index(this);
        }

        protected internal void RestoreFrom(Index backup)
        {
            base.RestoreFrom(backup);
            _tableSchema = backup.TableSchema;
            _tableName = backup.TableName;
            IsUnique = backup.IsUnique;
            IsImplicit = backup.IsImplicit;
            IndexType = backup.IndexType;
            Columns = (string[])backup.Columns.Clone();
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
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            Index idx = (Index)obj;
            return _tableSchema == idx.TableSchema
                && _tableName == idx.TableName
                && IsUnique == idx.IsUnique
                && IsImplicit == idx.IsImplicit
                && IndexType == idx.IndexType
                && ArrayEquals<string>(Columns, idx.Columns);
        }
        public override bool IsModified()
        {
            return (_backup != null) && ContentEquals(_backup);
        }

        public Index(Db2SourceContext context, string owner, string schema, string indexName, string tableSchema, string tableName, string[] columns, string definition) : base(context, owner, schema, indexName, Schema.CollectionIndex.Indexes)
        {
            //_schema = context.RequireSchema(schema);
            //_name = indexName;
            _tableSchema = tableSchema;
            _tableName = tableName;
            Columns = columns;
            //_definition = definition;
        }
        internal Index(Index basedOn): base(basedOn)
        {
            _tableSchema = basedOn.TableSchema;
            _tableName = basedOn.TableName;
            IsUnique = basedOn.IsUnique;
            IsImplicit = basedOn.IsImplicit;
            IndexType = basedOn.IndexType;
            Columns = (string[])basedOn.Columns.Clone();
        }
    }
}
