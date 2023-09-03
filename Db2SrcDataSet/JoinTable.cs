using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public enum JoinKind
    {
        Root,
        Inner,
        LeftOuter,
        RightOuter,
        FullOuter
    }
    public enum JoinDirection
    {
        ReferFrom,
        ReferTo,
    }
    public class JoinTable : INotifyPropertyChanged
    {
        private Selectable _table;
        public Selectable Table
        {
            get
            {
                return _table;
            }
            set
            {
                if (_table == value)
                {
                    return;
                }
                _table = value;
                InitVisibleColumns();
                OnPropertyChanged("Table");
            }
        }

        private string _alias;
        public string Alias
        {
            get
            {
                return _alias;
            }
            set
            {
                if (_alias == value)
                {
                    return;
                }
                _alias = value;
                OnPropertyChanged("Alias");
            }
        }

        private JoinTable _referrer;
        public JoinTable Referrer
        {
            get
            {
                return _referrer;
            }
            set
            {
                if (_referrer == value)
                {
                    return;
                }
                _referrer = value;
                OnPropertyChanged("Referrer");
            }
        }

        private JoinKind _kind;
        public JoinKind Kind
        {
            get
            {
                return _kind;
            }
            set
            {
                if (_kind == value)
                {
                    return;
                }
                _kind = value;
                OnPropertyChanged("Kind");
            }
        }

        private ForeignKeyConstraint _joinBy;
        public ForeignKeyConstraint JoinBy
        {
            get
            {
                return _joinBy;
            }
            set
            {
                if (_joinBy == value)
                {
                    return;
                }
                _joinBy = value;
                OnPropertyChanged("JoinBy");
            }
        }

        private JoinDirection _joinDirection;
        public JoinDirection JoinDirection
        {
            get
            {
                return _joinDirection;
            }
            set
            {
                if (_joinDirection == value)
                {
                    return;
                }
                _joinDirection = value;
                OnPropertyChanged("JoinDirection");
            }
        }

        private static readonly Column[] EmptyColumns = new Column[0];
        private Column[] _visibleColumns = EmptyColumns;
        public Column[] VisibleColumns
        {
            get
            {
                return _visibleColumns;
            }
            set
            {
                if (_visibleColumns == value)
                {
                    return;
                }
                _visibleColumns = value;
                OnPropertyChanged("VisibleColumns");
            }
        }
        public string[] JoinColumns { get; private set; }
        public string[] ReferrerColumns { get; private set; }

        private List<ForeignKeyConstraint> _selectableForeignKeys = new List<ForeignKeyConstraint>();
        public List<ForeignKeyConstraint> SelectableForeignKeys
        {
            get
            {
                return _selectableForeignKeys;
            }
        }

        private void InitVisibleColumns()
        {
            VisibleColumns = (_table != null) ? _table.Columns.GetVisibleColumns(HiddenLevel.Visible) : EmptyColumns;
        }

        public void UpdateSelectableForeignKeys(JoinTableCollection selected)
        {
            _selectableForeignKeys.Clear();
            Table tbl = Table as Table;
            if (tbl == null)
            {
                return;
            }
            foreach (ForeignKeyConstraint cons in tbl.ReferTo)
            {
                bool found = false;
                foreach (JoinTable jt in selected)
                {
                    if (jt.JoinDirection == JoinDirection.ReferFrom)
                    {
                        continue;
                    }
                    if (!tbl.Equals(jt.Referrer?.Table))
                    {
                        continue;
                    }
                    if (!cons.Equals(jt.JoinBy))
                    {
                        continue;
                    }
                    found = true;
                    break;
                }
                if (!found)
                {
                    _selectableForeignKeys.Add(cons);
                }
            }
            OnPropertyChanged("SelectableForeignKeys");
        }

        public string GetFieldsSQL(int indent, int charPerLine)
        {
            return Table.GetColumnsSQL(Alias, VisibleColumns, indent, charPerLine);
        }
        private static readonly Dictionary<JoinKind, string> JoinKindToSQL = new Dictionary<JoinKind, string>()
        {
            { JoinKind.Root, string.Empty },
            { JoinKind.Inner, "join " },
            { JoinKind.LeftOuter, "left outer join " },
            { JoinKind.RightOuter, "right outer join " },
            { JoinKind.FullOuter, "full outer join " }
        };

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public string GetJoinSQL(int indent, bool isFirst)
        {
            StringBuilder buf = new StringBuilder();
            if (Referrer == null || JoinBy == null || Kind == JoinKind.Root)
            {
                if (!isFirst)
                {
                    buf.AppendLine(",");
                    buf.Append(Db2SourceContext.IndentText);
                }
                buf.Append(Table.EscapedIdentifier(null));
                if (!string.IsNullOrEmpty(Alias))
                {
                    buf.Append(" as ");
                    buf.Append(Alias);
                }
                return buf.ToString();
            }
            if (!isFirst)
            {
                buf.AppendLine();
                buf.Append(Db2SourceContext.IndentText);
            }
            buf.Append(JoinKindToSQL[Kind]);
            string a = string.IsNullOrEmpty(Alias) ? string.Empty : Alias + ".";
            string aR = string.IsNullOrEmpty(Referrer.Alias) ? string.Empty : Referrer.Alias + ".";
            buf.Append(Table.EscapedIdentifier(null));
            if (!string.IsNullOrEmpty(Alias))
            {
                buf.Append(" as ");
                buf.Append(Alias);
            }
            buf.Append(" on (");
            string prefix = string.Empty;
            for (int i = 0; i < JoinColumns.Length; i++)
            {
                buf.Append(prefix);
                buf.Append(aR);
                buf.Append(ReferrerColumns[i]);
                buf.Append(" = ");
                buf.Append(a);
                buf.Append(JoinColumns[i]);
                prefix = " and ";
            }
            buf.Append(")");
            return buf.ToString();
        }
        public JoinTable()
        {
            InitVisibleColumns();
        }
        public JoinTable(JoinTable referrer, ForeignKeyConstraint constraint, JoinDirection direction)
        {
            _joinBy = constraint;
            _joinDirection = direction;
            Table tbl = constraint.Table;
            _referrer = referrer;
            if (referrer == null)
            {
                _kind = JoinKind.Root;
            }
            else
            {
                _kind = referrer.Kind;
                if (_kind == JoinKind.Root)
                {
                    _kind = JoinKind.Inner;
                }
                if (_kind != JoinKind.LeftOuter && _kind != JoinKind.FullOuter)
                {
                    foreach (string c in constraint.Columns)
                    {
                        Column col = tbl.Columns[c];
                        if (!col.NotNull)
                        {
                            _kind = JoinKind.LeftOuter;
                            break;
                        }
                    }
                }
            }
            switch (direction)
            {
                case JoinDirection.ReferFrom:
                    _table = constraint.Table;
                    ReferrerColumns = (string[])constraint.RefColumns.Clone();
                    JoinColumns = (string[])constraint.Columns.Clone();
                    break;
                case JoinDirection.ReferTo:
                    _table = constraint.ReferenceConstraint.Table;
                    ReferrerColumns = (string[])constraint.Columns.Clone();
                    JoinColumns = (string[])constraint.RefColumns.Clone();
                    break;
                default:
                    throw new NotImplementedException();
            }
            InitVisibleColumns();
        }
    }
    public class JoinTableCollection : ObservableCollection<JoinTable>
    {
        public string GetSelectSQL(string where, string orderBy, int? limit, int indent, int charPerLine, out int whereOffset)
        {
            string spc = Db2SourceContext.GetIndent(indent);
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.AppendLine("select");
            string delimiter = "," + Environment.NewLine;
            string prefix = string.Empty;
            foreach (JoinTable t in Items)
            {
                string s = t.GetFieldsSQL(indent + 1, charPerLine);
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }
                buf.Append(prefix);
                buf.Append(s);
                prefix = delimiter;
            }
            buf.AppendLine();
            buf.Append(spc);
            buf.Append("from ");
            bool isFirst = true;
            foreach (JoinTable t in Items)
            {
                buf.Append(t.GetJoinSQL(indent + 1, isFirst));
                isFirst = false;
            }
            buf.AppendLine();
            whereOffset = buf.Length;
            if (!string.IsNullOrEmpty(where))
            {
                buf.Append(spc);
                buf.Append("where ");
                whereOffset = buf.Length;
                buf.AppendLine(where);
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                buf.Append(spc);
                buf.Append("order by ");
                buf.AppendLine(orderBy);
            }
            if (limit.HasValue)
            {
                buf.Append(spc);
                buf.Append("limit ");
                buf.Append(limit.Value);
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public string GetSelectSQL(string where, string orderBy, int? limit, int indent, int charPerLine)
        {
            return GetSelectSQL(where, orderBy, limit, indent, charPerLine, out _);
        }
        public string GetSelectSQL(string[] where, string orderBy, int? limit, int indent, int charPerLine, out int whereOffset)
        {
            StringBuilder buf = new StringBuilder();
            bool needIndent = false;
            foreach (string s in where)
            {
                if (needIndent)
                {
                    buf.Append(Db2SourceContext.IndentText);
                }
                buf.AppendLine(s);
                needIndent = true;
            }
            return GetSelectSQL(buf.ToString(), orderBy, limit, indent, charPerLine, out whereOffset);
        }
        public string GetSelectSQL(string[] where, string orderBy, int? limit, int indent, int charPerLine)
        {
            int whereOffset;
            return GetSelectSQL(where, orderBy, limit, indent, charPerLine, out whereOffset);
        }
        public string GetSelectSQL(string where, string[] orderBy, int? limit, int indent, int charPerLine)
        {
            StringBuilder bufO = new StringBuilder();
            bool needComma = false;
            foreach (string s in orderBy)
            {
                if (needComma)
                {
                    bufO.Append(", ");
                }
                bufO.Append(s);
                needComma = true;
            }
            return GetSelectSQL(where, bufO.ToString(), limit, indent, charPerLine);
        }
        public string GetSelectSQL(string[] where, string[] orderBy, int? limit, int indent, int charPerLine, out int whereOffset)
        {
            StringBuilder bufW = new StringBuilder();
            bool needIndent = false;
            foreach (string s in where)
            {
                if (needIndent)
                {
                    bufW.Append(Db2SourceContext.IndentText);
                }
                bufW.AppendLine(s);
                needIndent = true;
            }
            StringBuilder bufO = new StringBuilder();
            bool needComma = false;
            foreach (string s in orderBy)
            {
                if (needComma)
                {
                    bufO.Append(", ");
                }
                bufO.Append(s);
                needComma = true;
            }
            return GetSelectSQL(bufW.ToString().TrimEnd(), bufO.ToString(), limit, indent, charPerLine, out whereOffset);
        }
        public string GetSelectSQL(string[] where, string[] orderBy, int? limit, int indent, int charPerLine)
        {
            int whereOffset;
            return GetSelectSQL(where, orderBy, limit, indent, charPerLine, out whereOffset);
        }
    }
}
