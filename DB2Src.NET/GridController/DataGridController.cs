using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Db2Source
{
    public class DataGridController: DependencyObject, IChangeSet
    {
        public static readonly DependencyProperty GridProperty = DependencyProperty.Register("Grid", typeof(DataGrid), typeof(DataGridController), new PropertyMetadata(new PropertyChangedCallback(OnGridPropertyChanged)));
        public static readonly DependencyProperty IsModifiedProperty = DependencyProperty.Register("IsModified", typeof(bool), typeof(DataGridController), new PropertyMetadata(new PropertyChangedCallback(OnIsModifiedPropertyChanged)));
        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register("SearchText", typeof(string), typeof(DataGridController), new PropertyMetadata(new PropertyChangedCallback(OnSearchTextPropertyChanged)));
        public static readonly DependencyProperty IgnoreCaseProperty = DependencyProperty.Register("IgnoreCase", typeof(bool), typeof(DataGridController), new PropertyMetadata(new PropertyChangedCallback(OnIgnoreCasePropertyChanged)));
        public static readonly DependencyProperty WordwrapProperty = DependencyProperty.Register("Wordwrap", typeof(bool), typeof(DataGridController), new PropertyMetadata(new PropertyChangedCallback(OnWordwrapPropertyChanged)));
        public static readonly DependencyProperty UseRegexProperty = DependencyProperty.Register("UseRegex", typeof(bool), typeof(DataGridController), new PropertyMetadata(new PropertyChangedCallback(OnUseRegexPropertyChanged)));
        public static readonly DependencyProperty UseSearchColumnProperty = DependencyProperty.Register("UseSearchColumn", typeof(bool), typeof(DataGridController), new PropertyMetadata(new PropertyChangedCallback(OnUseSearchColumnPropertyChanged)));
        public static readonly DependencyProperty SearchColumnProperty = DependencyProperty.Register("SearchColumn", typeof(DataGridColumn), typeof(DataGridController), new PropertyMetadata(new PropertyChangedCallback(OnSearchColumnPropertyChanged)));
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
            Dispatcher.InvokeAsync(UpdateIsModified, DispatcherPriority.ApplicationIdle);
        }

        public event EventHandler<DependencyPropertyChangedEventArgs> GridPropertyChanged;
        protected void OnGridPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InitGrid();
            GridPropertyChanged?.Invoke(this, e);
        }

        private static void OnGridPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DataGridController).OnGridPropertyChanged(e);
        }

        public void OnIsModifiedPropertyChanged(DependencyPropertyChangedEventArgs e)
        {

        }

        private static void OnIsModifiedPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DataGridController).OnIsModifiedPropertyChanged(e);
        }

        public void OnSearchTextPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateSearchEnd();
        }

        private static void OnSearchTextPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DataGridController).OnSearchTextPropertyChanged(e);
        }

        public void OnIgnoreCasePropertyChanged(DependencyPropertyChangedEventArgs e)
        {

        }

        private static void OnIgnoreCasePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DataGridController).OnIgnoreCasePropertyChanged(e);
        }

        public void OnWordwrapPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMatchTextProc();
        }

        private static void OnWordwrapPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DataGridController).OnWordwrapPropertyChanged(e);
        }

        public void OnUseRegexPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMatchTextProc();
        }

        private static void OnUseRegexPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DataGridController).OnUseRegexPropertyChanged(e);
        }

        public void OnUseSearchColumnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMatchTextProc();
        }

        private static void OnUseSearchColumnPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DataGridController).OnUseSearchColumnPropertyChanged(e);
        }

        public void OnSearchColumnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateMatchTextProc();
        }

        private static void OnSearchColumnPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            (target as DataGridController).OnSearchColumnPropertyChanged(e);
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
            DataGridColumn col = null;
            int i = -1;
            if (Grid.SelectedCells.Count != 0)
            {
                DataGridCellInfo cur = Grid.SelectedCells[0];
                i = Rows.IndexOf(cur.Item as Row);
                col = cur.Column;
            }
            if (i == -1)
            {
                i = Rows.Count;
            }
            if (col == null && Grid.Columns.Count != 0)
            {
                col = Grid.Columns[0];
            }
            Rows.Insert(i, row);
            Grid.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateSource();
            //Grid.ItemsSource = null;
            //Grid.ItemsSource = Rows;
            //UpdateGrid();
            if (row != null && col != null)
            {
                Dispatcher.InvokeAsync(() => { Grid.CurrentCell = new DataGridCellInfo(row, col); }, DispatcherPriority.Normal);
            }
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
        private string[] _keyFieldNames = StrUtil.EmptyStringArray;
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
                return _keyFields ?? ColumnInfo.EmptyArray;
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
		private Selectable _table;
		public Selectable Table
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
            Dispatcher.InvokeAsync(UpdateHasError, DispatcherPriority.ApplicationIdle);
        }

        private readonly Dictionary<string, ColumnInfo> _nameToField = new Dictionary<string, ColumnInfo>();
        public ColumnInfo GetFieldByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            ColumnInfo ret;
            if (_nameToField.TryGetValue(name.ToLower(), out ret))
            {
                return ret;
            }
            return null;
        }
        public void SetKeyFields(string[] keys)
        {
            if (keys == null)
            {
                _keyFieldNames = StrUtil.EmptyStringArray;
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
            Fields = ColumnInfo.EmptyArray;
            Rows = new RowCollection(this);
            BindingOperations.SetBinding(this, SearchTextProperty, new Binding("SearchText") { Source = MainWindow.Current });
            BindingOperations.SetBinding(this, IgnoreCaseProperty, new Binding("MatchByIgnoreCase") { Source = MainWindow.Current });
            BindingOperations.SetBinding(this, WordwrapProperty, new Binding("MatchByWordwrap") { Source = MainWindow.Current });
            BindingOperations.SetBinding(this, UseRegexProperty, new Binding("MatchByRegex") { Source = MainWindow.Current });
        }

        public async Task LoadAsync(Dispatcher dispatcher, IDataReader reader, CancellationToken cancellationToken)
        {
            int n = reader.FieldCount;
            Fields = new ColumnInfo[n];
            List<Row> rows = new List<Row>();
            for (int i = 0; i < n; i++)
            {
                ColumnInfo fi = new ColumnInfo(reader, i, null);
                Column c = Table?.Columns[fi.Name];
                if (c != null)
                {
                    fi.Comment = c.CommentText;
                    fi.StringFormat = c.StringFormat;
                }
                Fields[i] = fi;
                _nameToField[fi.Name.ToLower()] = fi;
            }
            while (reader.Read())
            {
                rows.Add(new Row(this, reader));
                cancellationToken.ThrowIfCancellationRequested();
            }
            await dispatcher.InvokeAsync(() =>
            {
                Rows = new RowCollection(this);
                foreach (Row row in rows)
                {
                    Rows.Add(row);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                IsModified = false;
            });
        }

        private void LoadInternal(IDataReader reader, Selectable table)
        {
            if (Grid != null)
            {
                Grid.ItemsSource = null;
            }
            int n = reader.FieldCount;
            Fields = new ColumnInfo[n];
            Rows = new RowCollection(this);
            for (int i = 0; i < n; i++)
            {
                ColumnInfo fi = new ColumnInfo(reader, i, table);
                Fields[i] = fi;
                _nameToField[fi.Name.ToLower()] = fi;
            }
            while (reader.Read())
            {
                Rows.Add(new Row(this, reader));
            }
            IsModified = false;
        }
        public async Task LoadAsync(Dispatcher dispatcher, IDataReader reader, Selectable table, CancellationToken cancellationToken)
        {
            Table = table;
            try
            {
                LoadInternal(reader, table);
            }
            finally
            {
                await UpdateGridAsync(dispatcher, cancellationToken);
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

        private bool IsTableEditable()
        {
            return (Table != null) && (Table.FirstCandidateKey != null) && (Table.FirstCandidateKey.Columns.Length != 0);
        }

        private void InitGrid()
        {
            Grid.IsVisibleChanged += Grid_IsVisibleChanged;
            Grid_IsVisibleChanged(Grid, new DependencyPropertyChangedEventArgs(UIElement.IsVisibleProperty, false, Grid.IsVisible));
            Grid.SelectedCellsChanged += Grid_SelectedCellsChanged;

            CommandBinding cb;
            Grid.CommandBindings.Clear();
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
            cb = new CommandBinding(DataGridCommands.CopyTableAsUpdate, CopyTableAsUpdateCommand_Executed, CopyTableCommand_CanExecute);
            Grid.CommandBindings.Add(cb);
            cb = new CommandBinding(DataGridCommands.CopyTableAsCopy, CopyTableAsCopyCommand_Executed, CopyTableCommand_CanExecute);
            Grid.CommandBindings.Add(cb);

            cb = new CommandBinding(DataGridCommands.CheckAll, CheckAllCommand_Executed, CheckAllCommand_CanExecute);
            Grid.CommandBindings.Add(cb);
            cb = new CommandBinding(DataGridCommands.UncheckAll, UncheckAllCommand_Executed, UncheckAllCommand_CanExecute);
            Grid.CommandBindings.Add(cb);
            cb = new CommandBinding(ApplicationCommands.Paste, PasteCommand_Executed, PasteCommand_CanExecute);
            Grid.CommandBindings.Add(cb);

            UpdateGrid();
        }

        private void UpdateGridColumns()
        {
            bool editable = IsTableEditable();
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
                    if (info.IsString)
                    {
                        c.EditingElementStyle = Application.Current.Resources["DataGridStringTextBoxStyle"] as Style;
                    }
                    else
                    {
                        c.EditingElementStyle = Application.Current.Resources["DataGridTextBoxStyle"] as Style;
                    }
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
        }
        public void UpdateGrid()
        {
            UpdateGridColumns();
            Grid.ItemsSource = null;
            Grid.ItemsSource = Rows;
        }
        public async Task UpdateGridAsync(Dispatcher dispatcher, CancellationToken cancellationToken)
        {
            await dispatcher.InvokeAsync(UpdateGrid);
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
                Dispatcher.CurrentDispatcher.InvokeAsync(win.Close);
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
        private static string EscapedText(string value, char[] EscapedChars)
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
            if (win != null)
            {
                if (win.IsVisible)
                {
                    win.Activate();
                    return;
                }
                win.Close();
            }
            win = new SearchDataGridControllerWindow();
            _searchDataGridTextWindow = new WeakReference<SearchDataGridControllerWindow>(win);
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

        private static readonly string[][] NoData = StrUtil.EmptyString2DArray;
        private string[][] GetCellData(bool includesHeader)
        {
            List<string[]> data = new List<string[]>();
            ColumnIndexInfo[] cols = GetDisplayColumns();
            if (cols.Length == 0)
            {
                return NoData;
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

        public static void CopyTableToClipboard(string[][] data)
        {
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

            foreach (string[] row in data)
            {
                bool needSeparator = false;
                foreach (string v in row)
                {
                    if (needSeparator)
                    {
                        tabText.Append('\t');
                        csvText.Append(',');
                    }
                    tabText.Append(EscapedText(v, TabTextEscapedChars));
                    csvText.Append(EscapedText(v, CsvEscapedChars));
                    needSeparator = true;
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
        private void DoCopyTable(bool includesHeader)
        {
            //DataGrid gr = sender as DataGrid;
            string[][] data = GetCellData(includesHeader);
            CopyTableToClipboard(data);
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
            if (!(Table is Table))
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
                buf.Append(DataSet.GetInsertSql(Table as Table, 0, 80, ";", values, true));
            }
            DataObject obj = new DataObject();
            obj.SetData(DataFormats.Text, buf.ToString());
            obj.SetData(DataFormats.UnicodeText, buf.ToString());
            Clipboard.SetDataObject(obj);
        }

        private void DoCopyTableAsUpdate()
        {
            if (!(Table is Table))
            {
                return;
            }
            Dictionary<object, Dictionary<ColumnInfo, object>> dict = new Dictionary<object, Dictionary<ColumnInfo, object>>();
            Dictionary<object, Dictionary<ColumnInfo, object>> dictKeys = new Dictionary<object, Dictionary<ColumnInfo, object>>();
            switch (Grid.SelectedCells.Count)
            {
                case 0:
                    return;
                case 1:
                    Row row;
                    Dictionary<ColumnInfo, object> values;
                    Dictionary<ColumnInfo, object> keys;
                    row = Grid.CurrentItem as Row;
                    if (row == null)
                    {
                        return;
                    }
                    values = new Dictionary<ColumnInfo, object>();
                    dict.Add(row, values);
                    keys = new Dictionary<ColumnInfo, object>();
                    dictKeys.Add(row, keys);
                    foreach (DataGridColumn col in Grid.Columns)
                    {
                        ColumnInfo c = col.Header as ColumnInfo;
                        if (c == null)
                        {
                            continue;
                        }
                        values.Add(c, row[c.Index]);
                    }
                    if (KeyFields != null)
                    {
                        foreach (ColumnInfo col in KeyFields)
                        {
                            keys.Add(col, row[col.Index]);
                        }
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
                        if (!dictKeys.TryGetValue(row, out keys))
                        {
                            keys = new Dictionary<ColumnInfo, object>();
                            if (KeyFields != null)
                            {
                                foreach (ColumnInfo c in KeyFields)
                                {
                                    keys.Add(c, row[c.Index]);
                                }
                            }
                            dictKeys.Add(row, keys);
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
                Dictionary<ColumnInfo, object> keys;
                if (!dict.TryGetValue(row, out values))
                {
                    continue;
                }
                if (!dictKeys.TryGetValue(row, out keys))
                {
                    keys = null;
                }
                buf.Append(DataSet.GetUpdateSql(Table as Table, 0, 80, ";", values, keys));
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

        private void CopyTableAsUpdateCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            DoCopyTableAsUpdate();
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
            e.CanExecute = (gr != null) && !gr.IsReadOnly && IsTableEditable();
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
            if (Rows.Count != 0)
            {
                Row last = Rows.Last();
                if (last.IsAdded)
                {
                    gr.ScrollIntoView(last);
                }
            }
        }
        private void CheckAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DataGrid gr = sender as DataGrid;
            e.CanExecute = (gr != null) && !gr.IsReadOnly && IsTableEditable();
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
                ((Row)item).IsDeleted = true;
            }
        }
        private void UncheckAllCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            DataGrid gr = sender as DataGrid;
            e.CanExecute = (gr != null) && !gr.IsReadOnly && IsTableEditable();
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
                ((Row)item).IsDeleted = false;
            }
        }

        public Row[] GetChanges()
        {
            List<Row> list = new List<Row>(Rows.Count);
            Dictionary<DataArray, Row> keyToRow = new Dictionary<DataArray, Row>();
            foreach (Row row in Rows)
            {
                if (row.ChangeKind != ChangeKind.None)
                {
                    list.Add(row);
                    keyToRow.Add(row.GetKeys(), row);
                }
            }
            List<Row> list2 = new List<Row>(list.Count);
            // 主キーの変更が競合しないように適用順序を調整する
            while (0 < list.Count)
            {
                Row row = list[0];
                list.RemoveAt(0);
                int p = list2.Count;
                list2.Add(row);
                Row row2 = row;
                DataArray key = row2.GetKeys();
                DataArray old = row2.GetOldKeys();
                keyToRow.Remove(key);
                while ((row2.IsAdded || row2.IsDeleted || !Equals(key, old)) && keyToRow.TryGetValue(old, out row2))
                {
                    list2.Insert(p, row2);
                    list.Remove(row2);
                    key = row2.GetKeys();
                    old = row2.GetOldKeys();
                    keyToRow.Remove(key);
                }
            }
            return list2.ToArray();
        }
        public void AcceptChanges()
        {
            Rows.AcceptChanges();
        }
    }
}
