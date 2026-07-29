using Microsoft.Office.Interop.Excel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using static Db2Source.GridClipboard;
using static Db2Source.NpgsqlDataSet.TableReturnType;

namespace Db2Source
{
    public class GridClipboard: INotifyPropertyChanged
    {
        public enum TextViewFormat
        {
            CSV,
            TabText,
        }
        public class Cell : INotifyPropertyChanged
        {
            public string Text { get; internal set; }
            private bool _isValid = true;
            public bool IsValid
            {
                get
                {
                    return _isValid;
                }
                internal set
                {
                    if (_isValid == value)
                    {
                        return;
                    }
                    _isValid = value;
                    OnPropertyChanged("IsValid");
                }
            }
            public object Data { get; internal set; }
            public Cell(string text)
            {
                Text = text;
                _isValid = true;
                Data = text;
            }

            internal void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
            public event PropertyChangedEventHandler PropertyChanged;
        }
        public enum CellDataType
        {
            None,
            Text,
            Numeric,
            Date,
            DateTime,
            Time,
            Boolean,
        }

        private static readonly Regex NumberRegex = new Regex(@"^[+-]?\d+(\.\d+)?$", RegexOptions.Compiled);
        private static CellDataType GuessCellDataType(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return CellDataType.None;
            }
            if (NumberRegex.IsMatch(text))
            {
                return CellDataType.Numeric;
            }
            if (DateTime.TryParse(text, out DateTime dt))
            {
                if (dt.TimeOfDay.TotalSeconds == 0)
                {
                    return CellDataType.Date;
                }
                else if (dt.Date == DateTime.MinValue.Date)
                {
                    return CellDataType.Time;
                }
                else
                {
                    return CellDataType.DateTime;
                }
            }
            if (bool.TryParse(text, out bool _))
            {
                return CellDataType.Boolean;
            }
            return CellDataType.Text;
        }
        public class Column: INotifyPropertyChanged
        {
            private static readonly CellDataType[,] OverlappedType = new CellDataType[,]
            {
                {CellDataType.None, CellDataType.Text, CellDataType.Numeric, CellDataType.Date, CellDataType.DateTime, CellDataType.Time, CellDataType.Boolean},
                {CellDataType.Text, CellDataType.Text, CellDataType.Text, CellDataType.Text, CellDataType.Text, CellDataType.Text, CellDataType.Text},
                {CellDataType.Numeric, CellDataType.Text, CellDataType.Numeric, CellDataType.Date, CellDataType.DateTime, CellDataType.Time, CellDataType.Numeric},
                {CellDataType.Date, CellDataType.Text, CellDataType.Date, CellDataType.Date, CellDataType.DateTime, CellDataType.DateTime, CellDataType.Text},
                {CellDataType.DateTime, CellDataType.Text, CellDataType.DateTime, CellDataType.DateTime, CellDataType.DateTime, CellDataType.DateTime, CellDataType.Text},
                {CellDataType.Time, CellDataType.Text, CellDataType.Time, CellDataType.DateTime, CellDataType.DateTime, CellDataType.Time, CellDataType.Text},
                {CellDataType.Boolean, CellDataType.Text, CellDataType.Numeric, CellDataType.Text, CellDataType.Text, CellDataType.Text, CellDataType.Boolean},
            };

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public int Index { get; private set; }
            private string _topText;
            public string Name
            {
                get
                {
                    return _hasHeader ? _topText : string.Format("(列{0})", Index + 1);
                }
            }

            private ColumnInfo _targetColumn;
            public ColumnInfo TargetColumn
            {
                get
                {
                    return _targetColumn;
                }
                set
                {
                    if (_targetColumn == value)
                    {
                        return;
                    }
                    _targetColumn = value;
                    OnPropertyChanged("TargetColumn");
                }
            }
            private bool _mayHaveHeader;
            public bool MayHaveHeader
            {
                get
                {
                    return _mayHaveHeader;
                }
                private set
                {
                    if (_mayHaveHeader == value)
                    {
                        return;
                    }
                    _mayHaveHeader = value;
                    OnPropertyChanged("MayHaveHeader");
                }
            }

            private bool _hasHeader;
            public bool HasHeader
            {
                get
                {
                    return _hasHeader;
                }
                set
                {
                    if (_hasHeader == value)
                    {
                        return;
                    }
                    _hasHeader = value;
                    OnPropertyChanged("HasHeader");
                    OnPropertyChanged("Name");
                }
            }

            public CellDataType DataType { get; private set; }

            public void GuessDataType(Row[] rows)
            {
                CellDataType guessed = CellDataType.None;
                for (int i = 1; i < rows.Length; i++)
                {
                    Row row = rows[i];
                    CellDataType t = GuessCellDataType(row.GetText(Index));
                    guessed = OverlappedType[(int)guessed, (int)t];
                }
                DataType = guessed;
                MayHaveHeader = false;
                _topText = string.Empty;
                Row row0 = rows[0];
                if (Index < row0.Count) {
                    MayHaveHeader = GuessCellDataType(row0.GetText(Index)) == CellDataType.Text && DataType != CellDataType.Text && DataType != CellDataType.None;
                    _topText = row0.GetText(Index);
                }
            }

            internal Column(int index)
            {
                Index = index;
                TargetColumn = null;
                MayHaveHeader = false;
                DataType = CellDataType.None;
            }
        }

        public class Row : IReadOnlyList<Cell>, INotifyPropertyChanged
        {
            public Cell[] Cells { get; private set; }
            private bool _isValid = true;
            public bool IsValid
            {
                get
                {
                    return _isValid;
                }
                private set
                {
                    if (_isValid == value)
                    {
                        return;
                    }
                    _isValid = value;
                    OnPropertyChanged("IsValid");
                }
            }

            internal Row(string[] values)
            {
                if (values == null)
                {
                    throw new ArgumentNullException("values");
                }
                Cells = new Cell[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    Cells[i] = new Cell(values[i]);
                }
            }

            internal void ResetColumns()
            {
                foreach (Cell cell in Cells)
                {
                    cell.IsValid = true;
                    cell.Data = cell.Text;
                }
                IsValid = true;
            }

            internal void CheckColumn(Column column)
            {
                if (0 <= column.Index && column.Index < Cells.Length)
                {
                    Cell cell = Cells[column.Index];
                    if (column.TargetColumn != null)
                    {
                        cell.Data = column.TargetColumn.ParseValue(cell.Text, false, out bool valid);
                        cell.IsValid = valid;
                    }
                    else
                    {
                        cell.Data = cell.Text;
                        cell.IsValid = true;
                    }
                }
                bool b = true;
                foreach (Cell cell in Cells)
                {
                    b &= cell.IsValid;
                }
                IsValid = b;
            }

            internal void CheckColumns(Column[] columns)
            {
                bool b = true;
                for (int i = 0; i < Cells.Length; i++)
                {
                    Column col = columns[i];
                    Cell cell = Cells[i];
                    try
                    {
                        if (col.TargetColumn != null)
                        {
                            cell.Data = col.TargetColumn.ParseValue(cell.Text, false, out bool valid);
                            cell.IsValid = valid;
                        }
                        else
                        {
                            cell.Data = cell.Text;
                            cell.IsValid = true;
                        }
                    }
                    catch
                    {
                        cell.IsValid = false;
                    }
                    b &= cell.IsValid;
                }
                IsValid = b;
            }

            public string GetText(int columnIndex)
            {
                if (columnIndex < 0 || Cells.Length <= columnIndex)
                {
                    return null;
                }
                return Cells[columnIndex].Text;
            }

            public object GetData(int columnIndex)
            {
                if (columnIndex < 0 || Cells.Length <= columnIndex)
                {
                    return null;
                }
                return Cells[columnIndex].Data;
            }

            internal void ApplyTo(Db2Source.Row target, Column[] columns, bool ignoreNull)
            {
                for (int i = 0; i < columns.Length; i++)
                {
                    Column col = columns[i];
                    if (col.TargetColumn == null)
                    {
                        continue;
                    }
                    object obj = GetData(i);
                    if (ignoreNull && (obj == null || (obj is string) && string.IsNullOrEmpty((string)obj)))
                    {
                        continue;
                    }
                    target[col.TargetColumn.Index] = obj;
                }
            }

            internal void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
            public event PropertyChangedEventHandler PropertyChanged;

            public DataArray GetDataArray(int[] indexes)
            {
                int n = indexes.Length;
                DataArray array = new DataArray(n);
                for (int i = 0; i < n; i++)
                {
                    array[i] = GetData(indexes[i]);
                }
                return array;
            }

            #region IReadOnlyList<Cell>の実装
            public Cell this[int index]
            {
                get
                {
                    return Cells[index];
                }
            }
            public int Count
            {
                get { return Cells.Length; }
            }
            public IEnumerator<Cell> GetEnumerator()
            {
                return ((IEnumerable<Cell>)Cells).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Cells.GetEnumerator();
            }
            #endregion
        }

        public Row[] Cells { get; private set; }

        public Column[] Columns { get; private set; }
        public int ColumnCount { get; private set; } = 0;
        public int MaxColumnCount { get; private set; } = 0;
        public bool IsSingleText { get; private set; } = false;

        private bool _isValid = false;
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
            private set
            {
                if (_isValid == value)
                {
                    return;
                }
                _isValid = value;
                OnPropertyChanged("IsValid");
            }
        }

        public DataGridController Controller { get; }

        private ColumnInfo[] _keyColumns;
        private int? _selectedKeyColumnCount;
        private object _selectedKeyColumnCountLock = new object();

        internal void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _hasHeader = false;
        public bool HasHeader
        {
            get
            {
                return _hasHeader;
            }
            set
            {
                if (_hasHeader == value)
                {
                    return;
                }
                _hasHeader = value;
                foreach (Column col in Columns)
                {
                    col.HasHeader = _hasHeader;
                }
                CheckColumns();
                OnPropertyChanged("HasHeader");
            }
        }

        public bool CanMergeByKey { get; private set; }

        private bool _mergeByKey = true;
        public bool MergeByKey
        {
            get
            {
                return _mergeByKey;
            }
            set
            {
                if (_mergeByKey == value)
                {
                    return;
                }
                _mergeByKey = value;
                OnPropertyChanged("MergeByKey");
            }
        }

        private bool _ignoreEmptyCell;
        public bool IgnoreEmptyCell
        {
            get
            {
                return _ignoreEmptyCell;
            }
            set
            {
                if (_ignoreEmptyCell == value)
                {
                    return;
                }
                _ignoreEmptyCell = value;
                OnPropertyChanged("IgnoreEmptyCell");
            }
        }

        private static object[] ToDataArray(ColumnInfo[] fields, string[] data)
        {
            if (fields.Length != data.Length)
            {
                throw new ArgumentException(string.Format("fieldsの要素数({0})とdataの要素数({1})が一致していません", fields.Length, data.Length));
            }
            int n = fields.Length;
            object[] ret = new object[n];
            for (int i = 0; i < n; i++)
            {
                bool valid;
                ret[i] = fields[0].ParseValue(data[i], false, out valid);
                if (!valid)
                {
                    ret[i] = new ValidationResult(false, data[i]);
                }
            }
            return ret;
        }

        private static object[] ExtractArray(object[] data, int[] indexes)
        {
            int n = indexes.Length;
            object[] ret = new object[n];
            for (int i = 0; i < n; i++)
            {
                ret[i] = data[indexes[i]];
            }
            return ret;
        }

        private int[] GetKeyIndexes()
        {
            Dictionary<ColumnInfo, int> dict = new Dictionary<ColumnInfo, int>();
            int n = _keyColumns.Length;
            int[] indexes = new int[n];
            for (int i = 0; i < n; i++)
            {
                ColumnInfo c = _keyColumns[i];
                dict[c] = i;
                indexes[i] = -1;
            }
            foreach (Column col in Columns)
            {
                if (col.TargetColumn != null && dict.ContainsKey(col.TargetColumn))
                {
                    indexes[dict[col.TargetColumn]] = col.Index;
                }
            }
            return indexes;
        }

        private void Merge()
        {
            int n = Cells.Length;
            int[] keyIdx = GetKeyIndexes();
            for (int i = HasHeader ? 1 : 0; i < n; i++)
            {
                Row row = Cells[i];
                DataArray keys = row.GetDataArray(keyIdx);
                Db2Source.Row target = Controller.Rows.FindRowByOldKey(keys);
                if (target == null)
                {
                    target = Controller.Rows.FindRowByKey(keys);
                }
                if (target == null)
                {
                    target = new Db2Source.Row(Controller);
                    Controller.Rows.Add(target);
                }
                row.ApplyTo(target, Columns, IgnoreEmptyCell);
            }
        }
        private void Overwrite()
        {
            DataGrid grid = Controller.Grid;
            object obj = grid.CurrentItem;
            int n = grid.Items.Count;
            int r0 = (obj != null) ? grid.Items.IndexOf(obj) : n;
            int i0 = HasHeader ? 1 : 0;
            for (int i = i0; i < Cells.Length; i++)
            {
                Row row = Cells[i];
                Db2Source.Row target = null;
                int r = r0 + i - i0;
                if (r < n)
                {
                    target = grid.Items[r] as Db2Source.Row;
                }
                if (target == null)
                {
                    target = new Db2Source.Row(Controller);
                    Controller.Rows.Add(target);
                }
                row.ApplyTo(target, Columns, IgnoreEmptyCell);
            }
        }

        public void Paste()
        {
            if (!IsValid)
            {
                return;
            }
            if (CanMergeByKey && MergeByKey)
            {
                Merge();
            }
            else
            {
                Overwrite();
            }
        }

        private static int CompareDataGridColumnByDisplayIndex(DataGridColumn x, DataGridColumn y)
        {
            return x.DisplayIndex.CompareTo(y.DisplayIndex);
        }

        private static ColumnInfo[] GetDisplayColumns(DataGrid grid)
        {
            List<DataGridColumn> l = new List<DataGridColumn>(grid.Columns);
            List<ColumnInfo> lRet = new List<ColumnInfo>();
            l.Sort(CompareDataGridColumnByDisplayIndex);
            foreach (DataGridColumn col in l)
            {
                if (col.DisplayIndex == -1 || col.Visibility != Visibility.Visible)
                {
                    continue;
                }
                ColumnInfo info = col.Header as ColumnInfo;
                if (info != null)
                {
                    lRet.Add(info);
                }
            }
            return lRet.ToArray();
        }

        //private static readonly int[] EmptyIntArray = new int[0];

        /// <summary>
        /// グリッドの選択位置からデータを貼り付けるように列割当てを行う
        /// 貼り付けるデータとグリッドの列数が一致している場合は列全体貼り付け
        /// 一致していない場合は選択列から貼り付け
        /// </summary>
        private void MatchByColumnPosition()
        {
            DataGrid grid = Controller.Grid;
            ColumnInfo[] infos = GetDisplayColumns(grid);
            int i0 = 0;
            ColumnInfo cur = grid.CurrentColumn?.Header as ColumnInfo;
            if (Columns.Length != infos.Length && cur != null)
            {
                for (int i = 0; i < infos.Length; i++)
                {
                    if (infos[i] == cur)
                    {
                        i0 = i;
                        break;
                    }
                }
            }
            int i1 = Math.Min(Columns.Length, infos.Length - i0);
            for (int i = 0; i < i1; i++)
            {
                Columns[i].TargetColumn = infos[i + i0];
            }
            for (int i = i1; i < Columns.Length; i++)
            {
                Columns[i].TargetColumn = null;
            }
        }

        internal string GetCellText(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || Cells.Length <= rowIndex || columnIndex < 0 || Cells[rowIndex].Count <= columnIndex)
            {
                return null;
            }
            return Cells[rowIndex][columnIndex].Text;
        }

        internal object GetCellData(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || Cells.Length <= rowIndex || columnIndex < 0 || Cells[rowIndex].Count <= columnIndex)
            {
                return null;
            }
            return Cells[rowIndex][columnIndex].Data;
        }

        private void CheckColumn(Column column)
        {
            int i0 = 0;
            if (HasHeader)
            {
                Cells[0].ResetColumns();
                i0 = 1;
            }
            for (int i = i0; i < Cells.Length; i++)
            {
                Row row = Cells[i];
                row.CheckColumn(column);
            }
            IsValid = true;
            foreach (Row row in Cells)
            {
                IsValid &= row.IsValid;
            }
        }

        private void CheckColumns()
        {
            int i0 = 0;
            if (HasHeader)
            {
                Cells[0].ResetColumns();
                i0 = 1;
            }
            for (int i = i0; i < Cells.Length; i++)
            {
                Row row = Cells[i];
                row.CheckColumns(Columns);
            }
            IsValid = true;
            foreach (Row row in Cells)
            {
                IsValid &= row.IsValid;
            }
        }

        private void MatchByColumnHeader()
        {
            foreach (Column col in Columns)
            {
                col.TargetColumn = Controller.GetFieldByName(GetCellText(0, col.Index)?.Trim());
            }
        }

        private void ColumnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TargetColumn")
            {
                InvalidateSelectedKeyColumnCount();
                if (sender  is Column column)
                {
                    CheckColumn(column);
                }
            }
        }

        private int RequireSelectedKeyColumnCount()
        {
            if (_selectedKeyColumnCount.HasValue)
            {
                return _selectedKeyColumnCount.Value;
            }
            lock (_selectedKeyColumnCountLock)
            {
                if (_selectedKeyColumnCount.HasValue)
                {
                    return _selectedKeyColumnCount.Value;
                }
                Dictionary<ColumnInfo, bool> dict = new Dictionary<ColumnInfo, bool>();
                foreach (ColumnInfo c in _keyColumns)
                {
                    dict[c] = false;
                }
                int n = 0;
                if (Columns != null)
                {
                    foreach (Column c in Columns)
                    {
                        if (c.TargetColumn != null && dict.ContainsKey(c.TargetColumn))
                        {
                            n++;
                        }
                    }
                }
                _selectedKeyColumnCount = n;
                return _selectedKeyColumnCount.Value;
            }
        }

        private void UpdateCanMergeByKey()
        {
            bool b = (0 < _keyColumns.Length) && (_keyColumns.Length == SelectedKeyColumnCount);
            if (CanMergeByKey == b)
            {
                return;
            }
            CanMergeByKey = b;
            OnPropertyChanged("CanMergeByKey");
        }

        private void InvalidateSelectedKeyColumnCount()
        {
            _selectedKeyColumnCount = null;
            UpdateCanMergeByKey();
        }

        internal int SelectedKeyColumnCount
        {
            get { return RequireSelectedKeyColumnCount(); }
        }

        private void GuessColumns()
        {
            if (Cells.Length == 0)
            {
                IsSingleText = true;
                return;
            }
            IsSingleText = (Cells.Length == 1 && Cells[0].Count == 1);

            Columns = new Column[MaxColumnCount];
            bool hasHeader = false;
            for (int i = 0; i < MaxColumnCount; i++)
            {
                Columns[i] = new Column(i);
                Columns[i].GuessDataType(Cells);
                Columns[i].PropertyChanged += ColumnPropertyChanged;
                hasHeader |= Columns[i].MayHaveHeader;
            }
            HasHeader = hasHeader;

            if (HasHeader)
            {
                MatchByColumnHeader();
            }
            // 先頭行がヘッダでない場合はヘッダによる照合を行わず、表示されている列の順序に従って照合する
            MatchByColumnPosition();
        }

        public GridClipboard(DataGridController controller, string[][] data)
        {
            if (controller == null)
            {
                throw new ArgumentNullException("controller");
            }
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            Controller = controller;
            _keyColumns = Controller.KeyFields;
            Cells = new Row[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                Cells[i] = new Row(data[i]);
            }
            if (0 < Cells.Length)
            {
                ColumnCount = Cells[0].Count;
                MaxColumnCount = ColumnCount;
                foreach (Row row in Cells)
                {
                    MaxColumnCount = Math.Max(MaxColumnCount, row.Count);
                }
            }
            GuessColumns();
        }
        public GridClipboard(DataGridController controller, string text, TextViewFormat format) : this(controller, GetArrayFromText(text, format)) { }

        public static string[][] GetArrayFromText(string text, TextViewFormat format)
        {
            switch (format)
            {
                case TextViewFormat.TabText:
                    return StrUtil.Get2DArrayFromTabText(text);
                case TextViewFormat.CSV:
                    return StrUtil.Get2DArrayFromCSV(text);
                default:
                    throw new ArgumentException("未対応のformatです");
            }
        }
    }
}
