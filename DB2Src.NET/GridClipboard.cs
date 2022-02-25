using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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

            internal void ApplyColumns(ColumnInfo[] columns)
            {
                if (Cells.Length != columns.Length)
                {
                    throw new ArgumentException("columnsの要素数が一致しません");
                }
                bool b = true;
                for (int i = 0; i < Cells.Length; i++)
                {
                    ColumnInfo col = columns[i];
                    Cell cell = Cells[i];
                    try
                    {
                        bool valid;
                        cell.Data = col.ParseValue(cell.Text, out valid);
                        cell.IsValid = valid;
                    }
                    catch
                    {
                        cell.IsValid = false;
                    }
                    b &= cell.IsValid;
                }
                IsValid = b;
            }

            internal void ApplyTo(Db2Source.Row target, ColumnInfo[] columns, bool ignoreNull)
            {
                for (int i = 0; i < columns.Length; i++)
                {
                    ColumnInfo col = columns[i];
                    object obj = Cells[i].Data;
                    if (ignoreNull && (obj == null || (obj is string) && string.IsNullOrEmpty((string)obj)))
                    {
                        continue;
                    }
                    target[col.Index] = obj;
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
                    array[i] = Cells[indexes[i]].Data;
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

        public DataGridController Controller { get; private set; }
        private ColumnInfo[] _explicitFields;
        private bool _isExplicitKeyEnabled;
        private int[] _explicitKeyIndexes;
        private ColumnInfo[] _explicitKeyFields;

        private ColumnInfo[] _implicitFields;
        private bool _isImplicitKeyEnabled;
        private int[] _implicitKeyIndexes;
        private ColumnInfo[] _implicitKeyFields;
        private bool _useExplicitHeader = false;

        internal void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasExplicitHeader { get; private set; }
        public bool UseExplicitHeader
        {
            get
            {
                return _useExplicitHeader;
            }
            set
            {
                bool v = value && HasExplicitHeader;
                if (_useExplicitHeader == v)
                {
                    return;
                }
                _useExplicitHeader = v;
                OnFieldsChanged();
                OnPropertyChanged("UseExplicitHeader");
                OnPropertyChanged("CanMergeByKey");
                MergeByKey = MergeByKey;
            }
        }

        private void OnFieldsChanged()
        {
            bool b = true;
            int i0 = 0;
            if (_useExplicitHeader)
            {
                Cells[0].ResetColumns();
                i0++;
            }
            for (int i = i0; i < Cells.Length; i++)
            {
                Row row = Cells[i];
                row.ApplyColumns(Fields);
                b &= row.IsValid;
            }
            IsValid = b;
        }

        public bool CanMergeByKey
        {
            get
            {
                return _useExplicitHeader ? _isExplicitKeyEnabled : _isImplicitKeyEnabled;
            }
        }

        private bool _mergeByKey;
        public bool MergeByKey
        {
            get
            {
                return _mergeByKey;
            }
            set
            {
                bool v = CanMergeByKey && value;
                if (_mergeByKey == v)
                {
                    return;
                }
                _mergeByKey = v;
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

        public ColumnInfo[] Fields
        {
            get
            {
                return _useExplicitHeader ? _explicitFields : _implicitFields;
            }
        }

        public ColumnInfo[] KeyFields
        {
            get
            {
                return _useExplicitHeader ? _explicitKeyFields : _implicitKeyFields;
            }
        }

        public int[] KeyIndexes
        {
            get
            {
                return _useExplicitHeader ? _explicitKeyIndexes : _implicitKeyIndexes;
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
                ret[i] = fields[0].ParseValue(data[i], out valid);
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

        private void Merge()
        {
            int n = Cells.Length;
            for (int i = UseExplicitHeader ? 1 : 0; i < n; i++)
            {
                Row row = Cells[i];
                DataArray keys = row.GetDataArray(KeyIndexes);
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
                row.ApplyTo(target, Fields, IgnoreEmptyCell);
            }
        }
        private void Overwrite()
        {
            DataGrid grid = Controller.Grid;
            object obj = grid.CurrentItem;
            int n = grid.Items.Count;
            int i0 = (obj != null) ? grid.Items.IndexOf(obj) : n;
            for (int i = UseExplicitHeader ? 1 : 0; i < Cells.Length; i++)
            {
                Row row = Cells[i];
                Db2Source.Row target = null;
                if (i0 + i < n)
                {
                    target = grid.Items[i0 + i] as Db2Source.Row;
                }
                if (target == null)
                {
                    target = new Db2Source.Row(Controller);
                    Controller.Rows.Add(target);
                }
                row.ApplyTo(target, Fields, IgnoreEmptyCell);
            }
        }

        public void Paste()
        {
            if (!IsValid)
            {
                return;
            }
            if (MergeByKey)
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

        private void InitExplicitFields()
        {
            _explicitFields = new ColumnInfo[ColumnCount];
            HasExplicitHeader = true;
            Dictionary<ColumnInfo, int> dict = new Dictionary<ColumnInfo, int>();
            for (int i = 0; i < ColumnCount; i++)
            {
                string s = Cells[0][i].Text?.Trim();
                ColumnInfo col = Controller.GetFieldByName(s);
                if (col == null)
                {
                    HasExplicitHeader = false;
                    break;
                }
                _explicitFields[i] = col;
                dict.Add(col, i);
            }
            if (!HasExplicitHeader)
            {
                _explicitFields = new ColumnInfo[0];
            }
            _useExplicitHeader = HasExplicitHeader;
            _explicitKeyFields = Controller.KeyFields;
            _explicitKeyIndexes = new int[_explicitKeyFields.Length];
            _isExplicitKeyEnabled = (0 < _explicitKeyFields.Length);
            for (int i = 0; i < _explicitKeyFields.Length; i++)
            {
                if (!dict.TryGetValue(_explicitKeyFields[i], out _explicitKeyIndexes[i]))
                {
                    _explicitKeyIndexes[i] = -1;
                    _isExplicitKeyEnabled = false;
                    break;
                }
            }
            if (!_isExplicitKeyEnabled)
            {
                _explicitKeyFields = new ColumnInfo[0];
                _explicitKeyIndexes = new int[0];
            }
            _mergeByKey = CanMergeByKey;
        }

        private void InitImplicitFields()
        {
            Dictionary<ColumnInfo, int> dict = new Dictionary<ColumnInfo, int>();
            DataGrid grid = Controller.Grid;
            ColumnInfo[] infos = GetDisplayColumns(grid);
            int i0 = (infos.Length == MaxColumnCount) ? 0 : (grid.CurrentColumn != null ? grid.CurrentColumn.DisplayIndex : 0);
            if (MaxColumnCount <= infos.Length - i0)
            {
                MaxColumnCount = infos.Length - i0;
                _implicitFields = null;
            }
            if (_implicitFields == null)
            {
                _implicitFields = new ColumnInfo[MaxColumnCount];
            }
            for (int i = 0; i < MaxColumnCount; i++)
            {
                ColumnInfo col = infos[i + i0];
                _implicitFields[i] = col;
                dict.Add(col, i);
            }
            _implicitKeyFields = Controller.KeyFields;
            _implicitKeyIndexes = new int[_implicitKeyFields.Length];
            _isImplicitKeyEnabled = (0 < _implicitKeyFields.Length);
            for (int i = 0; i < _implicitKeyFields.Length; i++)
            {
                if (!dict.TryGetValue(_implicitKeyFields[i], out _implicitKeyIndexes[i]))
                {
                    _implicitKeyIndexes[i] = -1;
                    _isImplicitKeyEnabled = false;
                    break;
                }
            }
            if (!_isImplicitKeyEnabled)
            {
                _implicitKeyFields = new ColumnInfo[0];
                _implicitKeyIndexes = new int[0];
            }
        }

        private void GuessFields()
        {
            if (Cells.Length == 0)
            {
                IsSingleText = true;
                return;
            }
            IsSingleText = (Cells.Length == 1 && Cells[0].Count == 1);
            InitExplicitFields();
            InitImplicitFields();
            OnFieldsChanged();
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
            GuessFields();
        }
        public GridClipboard(DataGridController controller, string text, TextViewFormat format) : this(controller, GetArrayFromText(text, format)) { }

        private static string[] SplitLines(string text, char quoteChar)
        {
            List<string> l = new List<string>();
            StringBuilder buf = new StringBuilder();
            bool inQuote = false;
            bool wasCr = false;
            int n = text.Length;
            for (int i = 0; i < n; i++)
            {
                char c = text[i];
                try
                {
                    // 改行コードCR, LF, CR+LFへの対応
                    if (!inQuote)
                    {
                        if (c == '\n' || c != '\n' && wasCr)
                        {
                            l.Add(buf.ToString());
                            buf.Clear();
                            continue;
                        }
                        if (c == '\r')
                        {
                            continue;
                        }
                    }
                    if (c == quoteChar)
                    {
                        inQuote = !inQuote;
                    }
                    buf.Append(c);
                }
                finally
                {
                    wasCr = (c == '\r');
                }
            }
            l.Add(buf.ToString());
            // 末尾の空行を除去
            for (int i = l.Count - 1; 0 <= i; i--)
            {
                if (string.IsNullOrEmpty(l[i]))
                {
                    l.RemoveAt(i);
                }
            }
            return l.ToArray();
        }
        public static string[][] GetArrayFromTabText(string text)
        {
            string[] lines = SplitLines(text, '\'');
            if (lines.Length == 0)
            {
                return StrUtil.EmptyString2DArray;
            }
            List<List<string>> lRet = new List<List<string>>();
            StringBuilder buf = new StringBuilder();
            foreach (string s in lines)
            {
                List<string> l = new List<string>();
                bool inQuote = false;
                bool wasQuote = false;
                int n = s.Length;
                for (int i = 0; i < n; i++)
                {
                    char c = s[i];
                    try
                    {
                        switch (c)
                        {
                            case '\'':
                                inQuote = !inQuote;
                                if (inQuote && wasQuote)
                                {
                                    buf.Append(c);
                                }
                                break;
                            case '\t':
                                if (inQuote)
                                {
                                    buf.Append(c);
                                }
                                else
                                {
                                    l.Add(buf.ToString());
                                    buf.Clear();
                                }
                                break;
                            default:
                                buf.Append(c);
                                break;
                        }
                    }
                    finally
                    {
                        wasQuote = (c == '\'');
                    }
                }
                l.Add(buf.ToString());
                buf.Clear();
                // 末尾の空文字列を除去
                for (int i = l.Count - 1; 0 <= i && string.IsNullOrEmpty(l[i]); i--)
                {
                    l.RemoveAt(i);
                }
                lRet.Add(l);
            }
            int nCol = lRet[0].Count;
            string[][] ret = new string[lRet.Count][];
            ret[0] = lRet[0].ToArray();
            // 先頭行より要素数の少ない行はnullで埋めて要素数を先頭行に合わせる
            for (int i = 1; i < lRet.Count; i++)
            {
                List<string> l = lRet[i];
                while (l.Count < nCol)
                {
                    l.Add(null);
                }
                ret[i] = l.ToArray();
            }
            return ret;
        }
        public static string[][] GetArrayFromCsv(string text)
        {
            string[] lines = SplitLines(text, '"');
            if (lines.Length == 0)
            {
                return StrUtil.EmptyString2DArray;
            }
            List<List<string>> lRet = new List<List<string>>();
            StringBuilder buf = new StringBuilder();
            foreach (string s in lines)
            {
                List<string> l = new List<string>();
                bool inQuote = false;
                bool wasQuote = false;
                int n = s.Length;
                for (int i = 0; i < n; i++)
                {
                    char c = s[i];
                    try
                    {
                        switch (c)
                        {
                            case '"':
                                inQuote = !inQuote;
                                if (inQuote && wasQuote)
                                {
                                    buf.Append(c);
                                }
                                break;
                            case ',':
                                if (inQuote)
                                {
                                    buf.Append(c);
                                }
                                else
                                {
                                    l.Add(buf.ToString());
                                    buf.Clear();
                                }
                                break;
                            default:
                                buf.Append(c);
                                break;
                        }
                    }
                    finally
                    {
                        wasQuote = (c == '"');
                    }
                }
                l.Add(buf.ToString());
                buf.Clear();
                // 末尾の空文字列を除去
                for (int i = l.Count - 1; 0 <= i && string.IsNullOrEmpty(l[i]); i--)
                {
                    l.RemoveAt(i);
                }
                lRet.Add(l);
            }
            int nCol = lRet[0].Count;
            string[][] ret = new string[lRet.Count][];
            ret[0] = lRet[0].ToArray();
            // 先頭行より要素数の少ない行はnullで埋めて要素数を先頭行に合わせる
            for (int i = 1; i < lRet.Count; i++)
            {
                List<string> l = lRet[i];
                while (l.Count < nCol)
                {
                    l.Add(null);
                }
                ret[i] = l.ToArray();
            }
            return ret;
        }
        public static string[][] GetArrayFromText(string text, TextViewFormat format)
        {
            switch (format)
            {
                case TextViewFormat.TabText:
                    return GetArrayFromTabText(text);
                case TextViewFormat.CSV:
                    return GetArrayFromCsv(text);
                default:
                    throw new ArgumentException("未対応のformatです");
            }
        }
    }
}
