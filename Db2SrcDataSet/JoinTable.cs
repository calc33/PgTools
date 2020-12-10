﻿using System;
using System.Collections;
using System.Collections.Generic;
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
    public class JoinTable
    {
        public Selectable Table { get; set; }
        public string Alias { get; set; }
        public JoinTable Referrer { get; set; }
        public JoinKind Kind { get; set; }
        public ForeignKeyConstraint JoinBy { get; set; }

        public string GetFieldsSQL(int indent, HiddenLevel visibleLevel)
        {
            return Table.GetColumnsSQL(Alias, visibleLevel);
        }
        private static readonly Dictionary<JoinKind, string> JoinKindToSQL = new Dictionary<JoinKind, string>()
        {
            { JoinKind.Root, string.Empty },
            { JoinKind.Inner, "join " },
            { JoinKind.LeftOuter, "left outer join " },
            { JoinKind.RightOuter, "right outer join " },
            { JoinKind.FullOuter, "full outer join " }
        };
        public string GetJoinSQL(int indent, bool isFirst)
        {
            StringBuilder buf = new StringBuilder();
            if (Referrer == null || JoinBy == null || Kind == JoinKind.Root)
            {
                if (!isFirst)
                {
                    buf.AppendLine(",");
                    buf.Append("  ");
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
                buf.Append("  ");
            }
            buf.Append(JoinKindToSQL[Kind]);
            buf.Append(Table.EscapedIdentifier(null));
            if (!string.IsNullOrEmpty(Alias))
            {
                buf.Append(" as ");
                buf.Append(Alias);
            }
            buf.Append(" on (");
            string a = string.IsNullOrEmpty(Alias) ? string.Empty : Alias + ".";
            string aR = string.IsNullOrEmpty(Referrer.Alias) ? string.Empty : Referrer.Alias + ".";
            string prefix = string.Empty;
            for (int i = 0; i < JoinBy.Columns.Length; i++)
            {
                buf.Append(prefix);
                buf.Append(aR);
                buf.Append(JoinBy.RefColumns[i]);
                buf.Append(" = ");
                buf.Append(a);
                buf.Append(JoinBy.Columns[i]);
                prefix = " and ";
            }
            buf.Append(")");
            return buf.ToString();
        }
    }
    public class JoinTableCollection: IList<JoinTable>, IList
    {
        private List<JoinTable> _list = new List<JoinTable>();

        public string GetSelectSQL(string where, string orderBy, int? limit, HiddenLevel visibleLevel, out int whereOffset)
        {
            StringBuilder buf = new StringBuilder();
            buf.AppendLine("select");
            string delimiter = "," + Environment.NewLine;
            string prefix = string.Empty;
            foreach (JoinTable t in _list)
            {
                string s = t.GetFieldsSQL(2, visibleLevel);
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }
                buf.Append(prefix);
                buf.Append(s);
                prefix = delimiter;
            }
            buf.AppendLine();
            buf.Append("from ");
            bool isFirst = true;
            foreach (JoinTable t in _list)
            {
                buf.Append(t.GetJoinSQL(2, isFirst));
                isFirst = false;
            }
            buf.AppendLine();
            whereOffset = buf.Length;
            if (!string.IsNullOrEmpty(where))
            {
                buf.Append("where ");
                whereOffset = buf.Length;
                buf.AppendLine(where);
            }
            if (!string.IsNullOrEmpty(orderBy))
            {
                buf.Append("order by ");
                buf.AppendLine(orderBy);
            }
            if (limit.HasValue)
            {
                buf.Append("limit ");
                buf.Append(limit.Value);
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public string GetSelectSQL(string where, string orderBy, int? limit, HiddenLevel visibleLevel)
        {
            int whereOffset = 0;
            return GetSelectSQL(where, orderBy, limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string[] where, string orderBy, int? limit, HiddenLevel visibleLevel, out int whereOffset)
        {
            StringBuilder buf = new StringBuilder();
            if (0 < where.Length)
            {
                buf.AppendLine(where[0]);
                for (int i = 1; i < where.Length; i++)
                {
                    buf.Append("  ");
                    buf.AppendLine(where[i]);
                }
            }
            return GetSelectSQL(buf.ToString(), orderBy, limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string[] where, string orderBy, int? limit, HiddenLevel visibleLevel)
        {
            int whereOffset = 0;
            return GetSelectSQL(where, orderBy, limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string where, string[] orderBy, int? limit, HiddenLevel visibleLevel)
        {
            StringBuilder bufO = new StringBuilder();
            if (0 < orderBy.Length)
            {
                bufO.Append(orderBy[0]);
                for (int i = 1; i < orderBy.Length; i++)
                {
                    bufO.Append(", ");
                    bufO.Append(orderBy[i]);
                }
            }
            return GetSelectSQL(where, bufO.ToString(), limit, visibleLevel);
        }
        public string GetSelectSQL(string[] where, string[] orderBy, int? limit, HiddenLevel visibleLevel, out int whereOffset)
        {
            StringBuilder bufW = new StringBuilder();
            if (0 < where.Length)
            {
                bufW.AppendLine(where[0]);
                for (int i = 1; i < where.Length; i++)
                {
                    bufW.Append("  ");
                    bufW.AppendLine(where[i]);
                }
            }
            StringBuilder bufO = new StringBuilder();
            if (0 < orderBy.Length)
            {
                bufO.Append(orderBy[0]);
                for (int i = 1; i < orderBy.Length; i++)
                {
                    bufO.Append(", ");
                    bufO.Append(orderBy[i]);
                }
            }
            return GetSelectSQL(bufW.ToString().TrimEnd(), bufO.ToString(), limit, visibleLevel, out whereOffset);
        }
        public string GetSelectSQL(string[] where, string[] orderBy, int? limit, HiddenLevel visibleLevel)
        {
            int whereOffset = 0;
            return GetSelectSQL(where, orderBy, limit, visibleLevel, out whereOffset);
        }

        #region IList<JoinTable>, IList の実装
        public JoinTable this[int index]
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

        object IList.this[int index]
        {
            get
            {
                return ((IList)_list)[index];
            }

            set
            {
                ((IList)_list)[index] = value;
            }
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return ((IList)_list).IsFixedSize;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return ((IList<JoinTable>)_list).IsReadOnly;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return ((IList)_list).IsSynchronized;
            }
        }

        public object SyncRoot
        {
            get
            {
                return ((IList)_list).SyncRoot;
            }
        }

        public int Add(object value)
        {
            return ((IList)_list).Add(value);
        }

        public void Add(JoinTable item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(object value)
        {
            return ((IList)_list).Contains(value);
        }

        public bool Contains(JoinTable item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }

        public void CopyTo(JoinTable[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<JoinTable> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public int IndexOf(JoinTable item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
        }

        public void Insert(int index, JoinTable item)
        {
            _list.Insert(index, item);
        }

        public void Remove(object value)
        {
            ((IList)_list).Remove(value);
        }

        public bool Remove(JoinTable item)
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
}