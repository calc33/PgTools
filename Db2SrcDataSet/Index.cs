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
        public Index(Db2SourceContext context, string owner, string schema, string indexName, string tableSchema, string tableName, string[] columns, string definition) : base(context, owner, schema, indexName, Schema.CollectionIndex.Indexes)
        {
            //_schema = context.RequireSchema(schema);
            //_name = indexName;
            _tableSchema = tableSchema;
            _tableName = tableName;
            Columns = columns;
            //_definition = definition;
        }
    }
}
