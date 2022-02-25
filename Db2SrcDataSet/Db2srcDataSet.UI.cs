using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Db2Source
{
    public class TreeNode
    {
        public string Name { get; set; }
        public string NameBase { get; set; }
        public int StyleIndex { get; set; }
        public string Hint { get; set; }
        public bool IsGrouped { get; set; }
        public bool ShowChildCount { get; set; }
        public Type TargetType { get; set; }
        public bool IsBold { get; set; }
        public bool IsHidden { get; set; }
        private NamedObject _target;
        public NamedObject Target
        {
            get
            {
                return _target;
            }
            set
            {
                if (_target == value)
                {
                    return;
                }
                NamedObject old = _target;
                _target = value;
                old?.RemoveNode(this);
                _target?.AddNode(this);
            }
        }
        public TreeNode[] Children { get; set; }
        public TreeNode(string name, string nameBase, Type targetType, int styleIndex, bool isBold, bool isHidden)
        {
            Name = name;
            NameBase = nameBase;
            StyleIndex = styleIndex;
            Hint = null;
            IsGrouped = true;
            IsBold = isBold;
            IsHidden = isHidden;
            TargetType = targetType;
            ShowChildCount = (NameBase != null);
            Target = null;
        }
        public TreeNode(SchemaObject target)
        {
            Name = target.DisplayName;
            StyleIndex = 0;
            Hint = target.CommentText;
            IsGrouped = false;
            IsHidden = false;
            ShowChildCount = false;
            TargetType = target.GetType();
            Target = target;
        }
    }
    partial class Db2SourceContext
    {
        public static string EscapedHeaderText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            StringBuilder buf = new StringBuilder();
            foreach (char c in value)
            {
                if (c == '_')
                {
                    buf.Append(c);
                }
                buf.Append(c);
            }
            return buf.ToString();
        }
        public static string UnescapedHeaderText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            StringBuilder buf = new StringBuilder();
            bool wasEsc = false;
            foreach (char c in value)
            {
                if (!wasEsc && c == '_')
                {
                    wasEsc = true;
                    continue;
                }
                buf.Append(c);
                wasEsc = false;
            }
            return buf.ToString();
        }

        public abstract TreeNode[] GetVisualTree();
        /// <summary>
        /// SQLの実行時例外からエラー位置を取得し、テキストボックスで選択する範囲を返す
        /// </summary>
        /// <param name="t">エラー</param>
        /// <param name="sql">テキストボックスに記載しているSQL</param>
        /// <param name="offset">テキストボックスに記載しているSQLが実行したSQLの一部であった場合、記載部分の開始位置</param>
        /// <returns>位置の取得に成功した場合(開始位置, 単語の長さ)、失敗した場合はnullを返す。</returns>
        public abstract Tuple<int, int> GetErrorPosition(Exception t, string sql, int offset);

        /// <summary>
        /// SQLの文字位置を指定し、そこにある単語を文字位置範囲で返す
        /// </summary>
        /// <param name="sql">テキストボックスに記載しているSQL</param>
        /// <param name="position">テキストボックス上のキャレットの位置</param>
        /// <returns>単語の取得に成功した場合(開始位置, 単語の長さ)、失敗した場合はnullを返す。</returns>
        public abstract Tuple<int, int> GetWordAt(string sql, int position);
        /// <summary>
        /// 例外のダイアログ出力用メッセージを取得
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public abstract string GetExceptionMessage(Exception t);
        public abstract bool SuggestsDropCascade(Exception t);
        public abstract bool AllowOutputParameter { get; }
    }

    public interface ISchemaObjectControl
    {
        SchemaObject Target { get; set; }
        string SelectedTabKey { get; set; }
        void OnTabClosing(object sender, ref bool cancel);
        void OnTabClosed(object sender);
    }

    partial class NamedObject
    {
        protected internal List<TreeNode> Nodes { get; } = new List<TreeNode>();
        /// <summary>
        /// ReferenceEqualsで一致したnodeのインデックスを返す
        /// </summary>
        /// <param name="node"></param>
        protected internal int IndexOfNode(TreeNode node)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (ReferenceEquals(Nodes[i], node))
                {
                    return i;
                }
            }
            return -1;
        }
        internal void AddNode(TreeNode node)
        {
            if (IndexOfNode(node) != -1)
            {
                return;
            }
            Nodes.Add(node);
        }

        internal void RemoveNode(TreeNode node)
        {
            int i = IndexOfNode(node);
            if (i == -1)
            {
                return;
            }
            Nodes.RemoveAt(i);
        }

        public object Control { get; set; }
        protected internal virtual void ReplaceTo(NamedObject target)
        {
            if (target == null)
            {
                return;
            }
            if (GetType() != target.GetType())
            {
                throw new ArgumentException();
            }
            if (target.Control == null && Control != null)
            {
                target.Control = Control;
            }
            Control = null;
            for (int i = Nodes.Count - 1; 0 <= i; i--)
            {
                TreeNode node = Nodes[i];
                if (node.Target == this)
                {
                    node.Target = target;
                    target.Nodes.Add(node);
                }
            }
            Nodes.Clear();
        }
    }
    partial class StoredFunction
    {
        public override string GetSqlType()
        {
            return "FUNCTION";
        }
        public override string GetExportFolderName()
        {
            return "Function";
        }
    }

    public enum HiddenLevel
    {
        Visible,
        Hidden,
        SystemInternal
    }

    public class HiddenLevelDisplayItem
    {
        public HiddenLevel Level { get; set; }
        public string Name { get; set; }
    }

    public class ColumnInfo
    {
        public static readonly ColumnInfo Stub = new ColumnInfo();
        public string Name { get; private set; }
        public HiddenLevel HiddenLevel { get; set; } = HiddenLevel.Visible;
        public bool IsNotNull { get; set; }
        /// <summary>
        /// Null許容型かどうかを返す、NOT NULL制約がかかっているかどうかはIsNotNull
        /// </summary>
        public bool IsNullable { get; private set; } = false;
        public bool AllowEmptyString { get; private set; } = true;
        public bool IsBoolean { get; private set; } = false;
        public bool IsNumeric { get; private set; } = false;
        public bool IsDateTime { get; private set; } = false;
        public bool IsArray { get; private set; } = false;
        public Type FieldType { get; private set; }
        public bool IsDefaultDefined { get; set; } = false;
        public Column Column { get; set; }
        public ForeignKeyConstraint[] ForeignKeys { get; set; }
        public string Comment { get; set; }
        //初期値についてはContext側に問い合わせてトリガーでセットするのかとかいろいろ取得する方向で
        public string DefaultValueExpr { get; set; }
        public string StringFormat { get; set; }
        //public IValueConverter Converter { get; set; }
        public int Index { get; private set; }
        public object ParseValue(string value, out bool valid)
        {
            valid = true;
            if (value == null)
            {
                return null;
            }
            Type refType = FieldType.MakeByRefType();
            MethodInfo mi = FieldType.GetMethod("TryParse", new Type[] { typeof(string), refType });
            if (mi != null)
            {
                try
                {
                    object[] args = new object[] { value, null };
                    bool ret = (bool)mi.Invoke(null, args);
                    if (!ret)
                    {
                        valid = false;
                        return null;
                    }
                    return args[1];
                }
                catch
                {
                    valid = false;
                    return null;
                }
            }
            if (FieldType == typeof(string) || FieldType.IsSubclassOf(typeof(string)))
            {
                return value;
            }
            throw new ArgumentException("value");
        }

        public object ConvertValue(object value)
        {
            if (value == null)
            {
                return DBNull.Value;
            }
            object ret = value;
            if (value is string)
            {
                bool valid;
                ret = ParseValue((string)value, out valid);
            }
            if (ret == null)
            {
                ret = DBNull.Value;
            }
            if (!AllowEmptyString && (ret as string) == string.Empty)
            {
                ret = DBNull.Value;
            }
            return ret;
        }
        private delegate object ValueConverter(object value, Type targetType, object parameter, CultureInfo culture);
        private ValueConverter _convert;
        private ValueConverter _convertBack;

        private object ConvertNone(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
        private object ConvertArray(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int l = 40;
            if (parameter is int)
            {
                l = (int)parameter;
            }
            else if (parameter is string)
            {
                int p;
                if (int.TryParse((string)parameter, out p))
                {
                    l = p;
                }
            }
            if (value == null)
            {
                return null;
            }
            if (!(value is IEnumerable))
            {
                return value;
            }
            StringBuilder lines = new StringBuilder();
            StringBuilder buf = new StringBuilder("(");
            bool needComma = false;
            foreach (object o in (IEnumerable)value)
            {
                if (needComma)
                {
                    buf.Append(',');
                    if (l <= buf.Length)
                    {
                        lines.AppendLine(buf.ToString());
                        buf = new StringBuilder();
                    }
                    buf.Append(' ');
                }
                needComma = true;
                buf.Append(o.ToString());
            }
            buf.Append(")");
            lines.Append(buf.ToString());
            return lines.ToString();
        }

        private object ConvertBackArray(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (!(value is IEnumerable))
            {
                return value;
            }
            string s = value.ToString().Trim();
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }
            if (!s.StartsWith("(") || !s.EndsWith(")"))
            {
                return null;
            }
            try
            {
                MethodInfo parseMethod = null;
                Type valueType = null;
                if (targetType.IsGenericType)
                {
                    valueType = targetType.GetGenericArguments()[0];
                    parseMethod = valueType.GetMethod("Parse", new Type[] { typeof(string) });
                }
                if (parseMethod == null)
                {
                    return null;
                }
                s = s.Substring(1, s.Length - 1);
                Type lt = typeof(List<>).MakeGenericType(new Type[] { valueType });
                IList l = lt.GetConstructor(Type.EmptyTypes) as IList;
                foreach (string v in s.Split(','))
                {
                    l.Add(parseMethod.Invoke(null, new object[] { v.Trim() }));
                }
                MethodInfo toArrayMethod = lt.GetMethod("ToArray", Type.EmptyTypes);
                return toArrayMethod.Invoke(l, null);
            }
            catch
            {
                return null;
            }
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return _convert(value, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return _convertBack(value, targetType, parameter, culture);
        }
        public ColumnInfo(IDataReader reader, int index)
        {
            Index = index;
            Name = reader.GetName(Index);
            Type ft = reader.GetFieldType(Index);
            if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                IsNullable = true;
                ft = ft.GetGenericArguments()[0];
            }
            IsBoolean = ft == typeof(bool);
            IsNumeric = ft == typeof(byte) || ft == typeof(sbyte) || ft == typeof(short) || ft == typeof(ushort)
                || ft == typeof(int) || ft == typeof(uint) || ft == typeof(long) || ft == typeof(ulong)
                || ft == typeof(float) || ft == typeof(double) || ft == typeof(decimal);
            AllowEmptyString = ft == typeof(string);
            IsDateTime = ft == typeof(DateTime);
            IsArray = ft == typeof(Array);
            FieldType = ft;
            _convert = ConvertNone;
            _convertBack = ConvertNone;
            if (IsArray)
            {
                _convert = ConvertArray;
                _convertBack = ConvertBackArray;
            }
        }
        public ColumnInfo()
        {
            Index = -1;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ColumnInfo))
            {
                return false;
            }
            ColumnInfo o = (ColumnInfo)obj;
            return Equals(Column, o.Column);
        }

        public override int GetHashCode()
        {
            if (Column == null)
            {
                return 0;
            }
            return Column.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}