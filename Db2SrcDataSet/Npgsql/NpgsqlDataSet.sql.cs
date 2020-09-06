using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Db2Source
{
    partial class NpgsqlDataSet
    {
        public override string GetSQL(Table table, string prefix, string postfix, int indent, bool addNewline, bool includePrimaryKey)
        {
            if (table == null)
            {
                return null;
            }
            if (table.Columns.Count == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.AppendFormat("create table {0} (", table.EscapedIdentifier(CurrentSchema));
            buf.AppendLine();
            int n = table.Columns.Count - 1;
            for (int i = 0; i < n; i++)
            {
                buf.Append(GetSQL(table.Columns[i], string.Empty, ",", indent + 2, true));
            }
            bool needKey = includePrimaryKey && (table.PrimaryKey != null);
            buf.Append(GetSQL(table.Columns[n], string.Empty, needKey ? "," : string.Empty, indent + 2, true));
            if (needKey)
            {
                buf.Append(GetSQL(table.PrimaryKey, string.Empty, string.Empty, indent + 2, true));
            }
            buf.Append(spc);
            buf.Append(")");
            if (ExportTablespace && !string.IsNullOrEmpty(table.TablespaceName))
            {
                buf.Append(" tablepslace ");
                buf.Append(table.TablespaceName);
            }
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public override string GetSQL(View table, string prefix, string postfix, int indent, bool addNewline)
        {
            if (table == null)
            {
                return null;
            }
            if (table.Columns.Count == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.AppendFormat("create or replace view {0} (", table.EscapedIdentifier(CurrentSchema));
            int colIndent = indent + 2;
            string colSpc = new string(' ', indent + 2);
            int l = colIndent;
            int n = table.Columns.Count - 1;
            for (int i = 0; i < n; i++)
            {
                string s = table.Columns[i].Name;
                if (i == 0 || PreferedCharsPerLine < l + s.Length + 1)
                {
                    buf.AppendLine();
                    buf.Append(colSpc);
                    l = colSpc.Length;
                }
                else
                {
                    buf.Append(' ');
                    l++;
                }
                buf.Append(s);
                buf.Append(',');
                l += s.Length + 1;
            }
            {
                string s = table.Columns[n].Name;
                if (PreferedCharsPerLine < l + s.Length + 1)
                {
                    buf.AppendLine();
                    buf.Append(colSpc);
                }
                else
                {
                    buf.Append(' ');
                    l++;
                }
                buf.Append(s);
                buf.Append(')');
            }
            buf.AppendLine();
            buf.AppendLine("AS");
            //buf.Append(spc);
            buf.Append(table.Definition);
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public override string GetSQL(Column column, string prefix, string postfix, int indent, bool addNewline)
        {
            if (column == null)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.Append(column.EscapedName);
            buf.Append(' ');
            buf.Append(column.DataType);
            //if (column.DataLength.HasValue)
            //{
            //    buf.Append('(');
            //    buf.Append(column.DataLength.Value);
            //    if (column.Precision.HasValue)
            //    {
            //        buf.Append(',');
            //        buf.Append(column.Precision.Value);
            //    }
            //    buf.Append(')');
            //}
            if (!string.IsNullOrEmpty(column.DefaultValue))
            {
                buf.Append(" default ");
                buf.Append(column.DefaultValue);
            }
            if (column.NotNull)
            {
                buf.Append(" not null");
            }
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }

        public static Dictionary<ConstraintType, string> InitConstraintTypeToStr()
        {
            Dictionary<ConstraintType, string> dict = new Dictionary<ConstraintType, string>();
            dict.Add(ConstraintType.Primary, "primary key");
            dict.Add(ConstraintType.Unique, "unique");
            dict.Add(ConstraintType.ForeignKey, "foreign key");
            dict.Add(ConstraintType.Check, "check");
            return dict;
        }
        public static readonly Dictionary<ConstraintType, string> ConstraintTypeToStr = InitConstraintTypeToStr();

        private string GetConstraintSqlBase(Constraint constraint, string prefix, int indent)
        {
            if (constraint.IsTemporaryName)
            {
                return string.Format("{0}{1} {2}", new string(' ', indent), prefix, ConstraintTypeToStr[constraint.ConstraintType]);
            }
            else
            {
                return string.Format("{0}{1}constraint {2} {3}", new string(' ', indent), prefix, GetEscapedIdentifier(constraint.Name), ConstraintTypeToStr[constraint.ConstraintType]);
            }
        }
        public override string GetSQL(KeyConstraint constraint, string prefix, string postfix, int indent, bool addNewline)
        {
            if (constraint == null)
            {
                return null;
            }
            if (constraint.Columns == null || constraint.Columns.Length == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(GetConstraintSqlBase(constraint, prefix, indent));
            string delim = " (";
            foreach (string c in constraint.Columns)
            {
                buf.Append(delim);
                buf.Append(c);
                delim = ", ";
            }
            buf.Append(')');
            if (constraint.Deferrable)
            {
                buf.Append(" deferrable");
            }
            if (constraint.Deferred)
            {
                buf.Append(" initially deferred");
            }
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }
        private static Dictionary<ForeignKeyRule, string> InitForeignKeyRuleToStr()
        {
            Dictionary<ForeignKeyRule, string> dict = new Dictionary<ForeignKeyRule, string>();
            dict.Add(ForeignKeyRule.NoAction, string.Empty);
            dict.Add(ForeignKeyRule.Restrict, "restrict");
            dict.Add(ForeignKeyRule.Cascade, "cascade");
            dict.Add(ForeignKeyRule.SetNull, "set null");
            return dict;
        }
        private static readonly Dictionary<ForeignKeyRule, string> ForeignKeyRuleToStr = InitForeignKeyRuleToStr();
        public override string GetSQL(ForeignKeyConstraint constraint, string prefix, string postfix, int indent, bool addNewline)
        {
            if (constraint == null)
            {
                return null;
            }
            if (constraint.Columns == null || constraint.Columns.Length == 0)
            {
                return null;
            }
            KeyConstraint rcons = constraint.ReferenceConstraint;
            if (rcons == null)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(GetConstraintSqlBase(constraint, prefix, indent));
            string delim = " (";
            foreach (string c in constraint.Columns)
            {
                buf.Append(delim);
                buf.Append(c);
                delim = ", ";
            }
            buf.Append(") references ");
            buf.Append(rcons.Table.EscapedIdentifier(constraint.TableSchema));
            if (rcons.ConstraintType == ConstraintType.Unique)
            {
                delim = "(";
                foreach (string c in rcons.Columns)
                {
                    buf.Append(delim);
                    buf.Append(c);
                    delim = ", ";
                }
                buf.Append(')');
            }
            string rule = ForeignKeyRuleToStr[constraint.UpdateRule];
            if (!string.IsNullOrEmpty(rule))
            {
                buf.Append(" on update ");
                buf.Append(rule);
            }
            rule = ForeignKeyRuleToStr[constraint.DeleteRule];
            if (!string.IsNullOrEmpty(rule))
            {
                buf.Append(" on delete ");
                buf.Append(rule);
            }
            if (constraint.Deferrable)
            {
                buf.Append(" deferrable");
            }
            if (constraint.Deferred)
            {
                buf.Append(" initially deferred");
            }
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public override string GetSQL(CheckConstraint constraint, string prefix, string postfix, int indent, bool addNewline)
        {
            if (constraint == null)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(GetConstraintSqlBase(constraint, prefix, indent));
            buf.Append(' ');
            buf.Append(constraint.Condition);
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public override string GetSQL(Constraint constraint, string prefix, string postfix, int indent, bool addNewline)
        {
            if (constraint == null)
            {
                return null;
            }
            if (constraint is KeyConstraint)
            {
                return GetSQL((KeyConstraint)constraint, prefix, postfix, indent, addNewline);
            }
            if (constraint is ForeignKeyConstraint)
            {
                return GetSQL((ForeignKeyConstraint)constraint, prefix, postfix, indent, addNewline);
            }
            if (constraint is CheckConstraint)
            {
                return GetSQL((CheckConstraint)constraint, prefix, postfix, indent, addNewline);
            }
            return null;
        }

        private delegate char ConvertChar(char ch);
        private static ConvertChar NormalizeIdentifierChar = char.ToLower;
        private static char NoConvert(char ch) { return ch; }
        private static string NormalizeIdentifier(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            StringBuilder buf = new StringBuilder();
            bool inQuote = false;
            foreach (char c in text)
            {
                buf.Append(inQuote ? c : NormalizeIdentifierChar(c));
                if (c == '"')
                {
                    inQuote = !inQuote;
                }
            }
            return buf.ToString();
        }
        private static void AppendToBuffer(StringBuilder buffer, string value, ref int colPos)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            buffer.Append(value);
            colPos += value.Length;
        }
        private static int GetLength(string value)
        {
            return value != null ? value.Length : 0;
        }
        private static int LINE_SOFTLIMIT = 80;
        private static int LINE_HARDLIMIT = 100;

        public override string GetSQL(Trigger trigger, string prefix, string postfix, int indent, bool addNewline)
        {
            if (trigger == null)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);

            buf.Append(spc);
            buf.Append(prefix);
            buf.AppendFormat("create trigger {0}", GetEscapedIdentifier(trigger.Name));
            buf.AppendLine();
            StringBuilder bufPart = new StringBuilder();
            spc += "  ";
            bufPart.Append(spc);
            bufPart.Append(trigger.TimingText);
            bufPart.Append(trigger.EventText);
            buf.Append(bufPart.ToString());
            int l = 0;
            int c = bufPart.Length;
            buf.Append(trigger.GetUpdateEventColumnsSql(" of ", spc + "  ", ref c, ref l, LINE_SOFTLIMIT, LINE_HARDLIMIT));
            bufPart = new StringBuilder();
            bufPart.Append("on ");
            bufPart.Append(GetEscapedIdentifier(trigger.TableSchema, trigger.TableName, CurrentSchema));
            if (c < LINE_SOFTLIMIT && c + bufPart.Length + 1 < LINE_HARDLIMIT && l == 0)
            {
                buf.Append(' ');
                buf.Append(bufPart.ToString());
            }
            else
            {
                buf.AppendLine();
                buf.Append(spc);
                buf.Append(bufPart.ToString());
            }
            buf.AppendLine();
            buf.Append(spc);
            buf.Append("for each ");
            buf.Append(trigger.OrientationText);
            buf.AppendLine();
            if (!string.IsNullOrEmpty(trigger.Condition))
            {
                buf.Append(spc);
                buf.Append("when ");
                buf.Append(trigger.Condition);
                buf.AppendLine();
            }
            buf.Append(spc);
            buf.Append(NormalizeIdentifier(trigger.Definition));
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public override string GetSQL(Index index, string prefix, string postfix, int indent, bool addNewline)
        {
            if (index == null)
            {
                return null;
            }
            if (index.IsImplicit)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            if (!string.IsNullOrEmpty(index.SqlDef))
            {
                buf.Append(index.SqlDef);
            }
            else
            {
                buf.Append("create ");
                if (index.IsUnique)
                {
                    buf.Append("unique ");
                }
                buf.Append("index ");
                buf.Append(GetEscapedIdentifier(index.Name));
                buf.Append(" on ");
                buf.Append(GetEscapedIdentifier(index.TableSchema, index.TableName, null));
                if (!string.IsNullOrEmpty(index.IndexType))
                {
                    buf.Append(" using ");
                    buf.Append(index.IndexType);
                    buf.Append(' ');
                }
                buf.Append('(');
                bool needComma = false;
                foreach (string c in index.Columns)
                {
                    if (needComma)
                    {
                        buf.Append(", ");
                    }
                    buf.Append(c);
                    needComma = true;
                }
                buf.Append(')');
            }
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }

        private string GetCommentSQL(string commentType, string identifier, string text, string prefix, string postfix, int indent, bool addNewline)
        {
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.AppendFormat("comment on {0} {1} is {2}", commentType.ToLower(), identifier, ToLiteralStr(text));
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public override string GetSQL(Comment comment, string prefix, string postfix, int indent, bool addNewline)
        {
            if (comment == null)
            {
                return null;
            }
            return GetCommentSQL(comment.GetCommentType(), comment.EscapedIdentifier(CurrentSchema), comment.Text, prefix, postfix, indent, addNewline);
        }

        public override string GetSQL(Sequence sequence, string prefix, string postfix, int indent, bool addNewline, bool ignoreOwned)
        {
            if (sequence == null)
            {
                return null;
            }
            if (ignoreOwned && !string.IsNullOrEmpty(sequence.OwnedColumn))
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);

            buf.Append(spc);
            buf.Append(prefix);
            buf.Append("create sequece ");
            buf.Append(GetEscapedIdentifier(sequence.Name));
            if (!string.IsNullOrEmpty(sequence.Increment))
            {
                buf.Append(" increment ");
                buf.Append(sequence.Increment);
            }
            if (!string.IsNullOrEmpty(sequence.MinValue))
            {
                buf.Append(" minvalue ");
                buf.Append(sequence.MinValue);
            }
            if (!string.IsNullOrEmpty(sequence.MaxValue))
            {
                buf.Append(" maxvalue ");
                buf.Append(sequence.MaxValue);
            }
            if (!string.IsNullOrEmpty(sequence.StartValue))
            {
                buf.Append(" start ");
                buf.Append(sequence.StartValue);
            }
            if (sequence.Cache != 1)
            {
                buf.Append(" cache ");
                buf.Append(sequence.Cache);
            }
            if (!string.IsNullOrEmpty(sequence.OwnedColumn))
            {
                buf.Append(" owned by ");
                buf.Append(GetEscapedIdentifier(sequence.OwnedSchema, sequence.OwnedTable, sequence.SchemaName));
                buf.Append('.');
                buf.Append(GetEscapedIdentifier(sequence.OwnedColumn));
            }
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }

        //private static readonly string[] ParameterDirectionStr = { "", "", "out ", "inout ", "variadic ", "variadic ", "variadic out ", "variadic inout ", "result " };
        private static readonly string[] ParameterDirectionStr = { string.Empty, string.Empty, "out ", "inout ", string.Empty, string.Empty, "result " };
        private string GetParameterDirectionStr(ParameterDirection direction)
        {
            int i = (int)direction;
            if (i < 0 || ParameterDirectionStr.Length <= i)
            {
                return null;
            }
            return ParameterDirectionStr[i];
        }
        public override string GetSQL(Parameter p)
        {
            StringBuilder buf = new StringBuilder();
            buf.Append(GetParameterDirectionStr(p.Direction));
            if (!string.IsNullOrEmpty(p.Name))
            {
                buf.Append(GetEscapedIdentifier(p.Name));
                buf.Append(' ');
            }
            buf.Append(p.DataType);
            if (!string.IsNullOrEmpty(p.DefaultValue))
            {
                buf.Append(" = ");
                buf.Append(p.DefaultValue);
                //if (p.ValueType == typeof(string) || p.ValueType.IsSubclassOf(typeof(string)))
                //{
                //    buf.Append(GetQuotedStr(p.DefaultValue));
                //}
                //else
                //{
                //    buf.Append(p.DefaultValue);
                //}
            }
            return buf.ToString();
        }
        public override string GetSQL(StoredFunction function, string prefix, string postfix, int indent, bool addNewline)
        {
            string spc = new string(' ', indent);
            StringBuilder buf = new StringBuilder();
            buf.Append(spc);
            buf.Append(prefix);
            buf.Append("create or replace function ");
            buf.Append(function.EscapedIdentifier(CurrentSchema));
            buf.Append('(');
            bool needComma = false;
            foreach (Parameter p in function.Parameters)
            {
                if (needComma)
                {
                    buf.Append(", ");
                }
                buf.Append(GetSQL(p));
                needComma = true;
            }
            buf.Append(')');
            buf.AppendLine();
            buf.Append(spc);
            buf.Append(" returns ");
            buf.AppendLine(function.DataType);
            if (!string.IsNullOrEmpty(function.Language))
            {
                buf.Append(spc);
                buf.Append(" language ");
                buf.AppendLine(function.Language);
            }
            buf.Append(spc);
            buf.Append(" as $function$"); // Definitionの前後に改行が含まれるためここでは改行しない
            buf.Append(function.Definition); // Definitionの前後に改行が含まれるためここでは改行しない
            buf.Append(spc);
            buf.Append("$function$");
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }
        //public override string GetSQL(StoredProcedure procedure, string prefix, string postfix, int indent, bool addNewline)
        //{

        //}

        public override string[] GetAlterTableSQL(Table after, Table before)
        {
            throw new NotImplementedException();
        }
        public override string[] GetAlterViewSQL(View after, View before)
        {
            if (after == null)
            {
                if (before == null)
                {
                    return new string[0];
                }
                return new string[] { string.Format("drop view {0}", before.EscapedIdentifier(CurrentSchema)) };
            }
            return new string[] { GetSQL(after, string.Empty, ";", 0, false) };
        }

        public override string[] GetAlterColumnSQL(Column after, Column before)
        {
            Db2SourceContext ctx;
            if (after == null)
            {
                if (before == null)
                {
                    return new string[0];
                }
                //ctx = before.Context;
                return new string[] { string.Format("alter table {0} drop column {1}", before.EscapedIdentifier(CurrentSchema), before.EscapedName) };
            }
            ctx = after.Context;
            string tbl = after.Table.EscapedIdentifier(CurrentSchema);
            string col = after.EscapedName;
            if (before == null)
            {
                return new string[] {
                    string.Format("alter table {0} add {1}", tbl, ctx.GetSQL(after, string.Empty, string.Empty, 0, false))
                };
            }
            List<string> list = new List<string>();
            if (after.DataType != before.DataType)
            {
                list.Add(string.Format("alter table {0} alter {1} type {2}", tbl, col, after.DataType));
            }
            if (after.DefaultValue != before.DefaultValue)
            {
                if (string.IsNullOrEmpty(after.DefaultValue))
                {
                    list.Add(string.Format("alter table {0} alter {1} drop default", tbl, col));
                }
                else
                {
                    list.Add(string.Format("alter table {0} alter {1} set default {2}", tbl, col, after.DefaultValue));
                }
            }
            if (after.NotNull != before.NotNull)
            {
                if (after.NotNull)
                {
                    list.Add(string.Format("alter table {0} alter {1} set not null", tbl, col));
                }
                else
                {
                    list.Add(string.Format("alter table {0} alter {1} drop not null", tbl, col));
                }
            }
            return list.ToArray();
        }

        public override string[] GetAlterCommentSQL(Comment after, Comment before)
        {
            if (after == null)
            {
                if (before == null)
                {
                    return new string[0];
                }
                return new string[] { GetCommentSQL(before.GetCommentType(), before.EscapedIdentifier(CurrentSchema), null, string.Empty, string.Empty, 0, false) };
            }
            return new string[] { GetSQL(after, string.Empty, string.Empty, 0, false) };
        }

        public override string GetSQL(ComplexType type, string prefix, string postfix, int indent, bool addNewline)
        {
            if (type == null)
            {
                return null;
            }
            if (type.Columns.Count == 0)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.AppendFormat("create type {0} AS (", type.EscapedIdentifier(CurrentSchema));
            buf.AppendLine();
            int n = type.Columns.Count - 1;
            for (int i = 0; i < n; i++)
            {
                buf.Append(GetSQL(type.Columns[i], string.Empty, ",", indent + 2, true));
            }
            buf.Append(GetSQL(type.Columns[n], string.Empty, string.Empty, indent + 2, true));
            buf.Append(spc);
            buf.Append(")");
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }
        public override string GetSQL(EnumType type, string prefix, string postfix, int indent, bool addNewline)
        {
            return string.Empty;
        }
        public override string GetSQL(RangeType type, string prefix, string postfix, int indent, bool addNewline)
        {
            return string.Empty;
        }
        public override string GetSQL(BasicType type, string prefix, string postfix, int indent, bool addNewline)
        {
            if (type == null)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.AppendFormat("create type {0} (", type.EscapedIdentifier(CurrentSchema));
            buf.AppendLine();
            buf.Append(spc);
            buf.Append("  input = ");
            buf.Append(type.InputFunction);
            buf.AppendLine(",");
            buf.Append(spc);
            buf.Append("  output = ");
            buf.Append(type.OutputFunction);
            if (!string.IsNullOrEmpty(type.ReceiveFunction))
            {
                buf.AppendLine(",");
                buf.Append(spc);
                buf.Append("  receive = ");
                buf.Append(type.ReceiveFunction);
            }
            if (!string.IsNullOrEmpty(type.SendFunction))
            {
                buf.AppendLine(",");
                buf.Append(spc);
                buf.Append("  send = ");
                buf.Append(type.SendFunction);
            }
            if (!string.IsNullOrEmpty(type.TypmodInFunction))
            {
                buf.AppendLine(",");
                buf.Append(spc);
                buf.Append("  typmod_in = ");
                buf.Append(type.TypmodInFunction);
            }
            if (!string.IsNullOrEmpty(type.TypmodOutFunction))
            {
                buf.AppendLine(",");
                buf.Append(spc);
                buf.Append("  typmod_out = ");
                buf.Append(type.TypmodOutFunction);
            }
            if (!string.IsNullOrEmpty(type.AnalyzeFunction))
            {
                buf.AppendLine(",");
                buf.Append(spc);
                buf.Append("  analyze = ");
                buf.Append(type.AnalyzeFunction);
            }
            if (!string.IsNullOrEmpty(type.InternalLengthFunction))
            {
                buf.AppendLine(",");
                buf.Append(spc);
                buf.Append("  internallength = ");
                buf.Append(type.InternalLengthFunction);
            }
            if (type.PassedbyValue)
            {
                buf.AppendLine(",");
                buf.Append(spc);
                buf.Append("  passedbyvalue");
            }
            if (type.Alignment != 0)
            {
                buf.AppendLine(",");
                buf.Append(spc);
                buf.Append("  alignment = ");
                buf.Append(type.Alignment);
            }
            if (!string.IsNullOrEmpty(type.Storage))
            {
                buf.AppendLine(",");
                buf.Append(spc);
                buf.Append("  storage = ");
                buf.Append(type.Storage);
            }
            buf.AppendLine();
            buf.Append(spc);
            buf.Append(")");
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return buf.ToString();
        }
    }
}
