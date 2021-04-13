using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Npgsql;

namespace Db2Source
{
    partial class NpgsqlDataSet
    {
        private static string[] NoSQL = new string[0];
        private static string _Expand(string[] strs, string delimiter = "")
        {
            if (strs == null || strs.Length == 0)
            {
                return string.Empty;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(strs[0]);
            for (int i = 1; i < strs.Length; i++)
            {
                buf.Append(delimiter);
                buf.Append(strs[i]);
            }
            return buf.ToString();
        }
        public override string[] GetSQL(Table table, string prefix, string postfix, int indent, bool addNewline, bool includePrimaryKey)
        {
            if (table == null)
            {
                return NoSQL;
            }
            if (table.Columns.Count == 0)
            {
                return NoSQL;
            }
            List<string> l = new List<string>();
            foreach (Sequence seq in table.Sequences)
            {
                l.AddRange(GetSQL(seq, prefix, postfix, indent, addNewline, false, true));
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
                buf.Append(_Expand(GetSQL(table.Columns[i], string.Empty, ",", indent + 2, true)));
            }
            bool needKey = includePrimaryKey && (table.PrimaryKey != null);
            buf.Append(_Expand(GetSQL(table.Columns[n], string.Empty, needKey ? "," : string.Empty, indent + 2, true)));
            if (needKey)
            {
                buf.Append(_Expand(GetSQL(table.PrimaryKey, string.Empty, string.Empty, indent + 2, false, true)));
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
            l.Add(buf.ToString());
            foreach (Sequence seq in table.Sequences)
            {
                l.Add(string.Format("{0}alter sequence {1} owner to {2};{3}", spc, seq.EscapedIdentifier(CurrentSchema), table.EscapedIdentifier(CurrentSchema), addNewline ? Environment.NewLine : string.Empty));
            }

            return l.ToArray();
        }
        public override string[] GetSQL(View table, string prefix, string postfix, int indent, bool addNewline)
        {
            if (table == null)
            {
                return NoSQL;
            }
            if (table.Columns.Count == 0)
            {
                return NoSQL;
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
                if (i == 0 || PreferredCharsPerLine < l + s.Length + 1)
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
                if (PreferredCharsPerLine < l + s.Length + 1)
                {
                    buf.AppendLine();
                    buf.Append(colSpc);
                }
                else
                {
                    buf.Append(' ');
                    //l++;
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
            return new string[] { buf.ToString() };
        }
        public override string[] GetSQL(Column column, string prefix, string postfix, int indent, bool addNewline)
        {
            if (column == null)
            {
                return NoSQL;
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
            return new string[] { buf.ToString() };
        }

        public static Dictionary<ConstraintType, string> InitConstraintTypeToStr()
        {
            Dictionary<ConstraintType, string> dict = new Dictionary<ConstraintType, string>
            {
                { ConstraintType.Primary, "primary key" },
                { ConstraintType.Unique, "unique" },
                { ConstraintType.ForeignKey, "foreign key" },
                { ConstraintType.Check, "check" }
            };
            return dict;
        }
        public static readonly Dictionary<ConstraintType, string> ConstraintTypeToStr = InitConstraintTypeToStr();

        private string GetConstraintSqlBase(Constraint constraint, string prefix, int indent, bool addAlterTable)
        {
            string pre;
            if (addAlterTable)
            {
                pre = string.Format("{0}alter table {1} add ", prefix, constraint.Table.EscapedIdentifier(CurrentSchema));
            }
            else
            {
                pre = prefix;
            }
            if (constraint.IsTemporaryName)
            {
                return string.Format("{0}{1} {2}", new string(' ', indent), pre, ConstraintTypeToStr[constraint.ConstraintType]);
            }
            else
            {
                return string.Format("{0}{1}constraint {2} {3}", new string(' ', indent), pre, GetEscapedIdentifier(constraint.Name), ConstraintTypeToStr[constraint.ConstraintType]);
            }
        }
        public override string[] GetSQL(KeyConstraint constraint, string prefix, string postfix, int indent, bool addAlterTable, bool addNewline)
        {
            if (constraint == null)
            {
                return NoSQL;
            }
            if (constraint.Columns == null || constraint.Columns.Length == 0)
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(GetConstraintSqlBase(constraint, prefix, indent, addAlterTable));
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
            return new string[] { buf.ToString() };
        }
        private static Dictionary<ForeignKeyRule, string> InitForeignKeyRuleToStr()
        {
            Dictionary<ForeignKeyRule, string> dict = new Dictionary<ForeignKeyRule, string>
            {
                { ForeignKeyRule.NoAction, string.Empty },
                { ForeignKeyRule.Restrict, "restrict" },
                { ForeignKeyRule.Cascade, "cascade" },
                { ForeignKeyRule.SetNull, "set null" }
            };
            return dict;
        }
        private static readonly Dictionary<ForeignKeyRule, string> ForeignKeyRuleToStr = InitForeignKeyRuleToStr();
        public override string[] GetSQL(ForeignKeyConstraint constraint, string prefix, string postfix, int indent, bool addAlterTable, bool addNewline)
        {
            if (constraint == null)
            {
                return NoSQL;
            }
            if (constraint.Columns == null || constraint.Columns.Length == 0)
            {
                return NoSQL;
            }
            KeyConstraint rcons = constraint.ReferenceConstraint;
            if (rcons == null)
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(GetConstraintSqlBase(constraint, prefix, indent, addAlterTable));
            string delim = " (";
            foreach (string c in constraint.Columns)
            {
                buf.Append(delim);
                buf.Append(c);
                delim = ", ";
            }
            buf.Append(") references ");
            buf.Append(rcons.Table.EscapedIdentifier(CurrentSchema));
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
            return new string[] { buf.ToString() };
        }
        public override string[] GetSQL(CheckConstraint constraint, string prefix, string postfix, int indent, bool addAlterTable, bool addNewline)
        {
            if (constraint == null)
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(GetConstraintSqlBase(constraint, prefix, indent, addAlterTable));
            buf.Append(' ');
            buf.Append(constraint.Condition);
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }
        public override string[] GetSQL(Constraint constraint, string prefix, string postfix, int indent, bool addAlterTable, bool addNewline)
        {
            if (constraint == null)
            {
                return NoSQL;
            }
            if (constraint is KeyConstraint)
            {
                return GetSQL((KeyConstraint)constraint, prefix, postfix, indent, addAlterTable, addNewline);
            }
            if (constraint is ForeignKeyConstraint)
            {
                return GetSQL((ForeignKeyConstraint)constraint, prefix, postfix, indent, addAlterTable, addNewline);
            }
            if (constraint is CheckConstraint)
            {
                return GetSQL((CheckConstraint)constraint, prefix, postfix, indent, addAlterTable, addNewline);
            }
            return NoSQL;
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
        private static readonly int LINE_SOFTLIMIT = 80;
        private static readonly int LINE_HARDLIMIT = 100;

        public override string[] GetSQL(Trigger trigger, string prefix, string postfix, int indent, bool addNewline)
        {
            if (trigger == null)
            {
                return NoSQL;
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
            return new string[] { buf.ToString() };
        }
        public override string[] GetSQL(Index index, string prefix, string postfix, int indent, bool addNewline)
        {
            if (index == null)
            {
                return NoSQL;
            }
            if (index.IsImplicit)
            {
                return NoSQL;
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
                buf.Append(GetEscapedIdentifier(index.TableSchema, index.TableName, CurrentSchema));
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
            return new string[] { buf.ToString() };
        }

        private string[] GetCommentSQL(string commentType, string identifier, string text, string prefix, string postfix, int indent, bool addNewline)
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
            return new string[] { buf.ToString() };
        }
        public override string[] GetSQL(Comment comment, string prefix, string postfix, int indent, bool addNewline)
        {
            if (comment == null)
            {
                return NoSQL;
            }
            return GetCommentSQL(comment.GetCommentType(), comment.EscapedIdentifier(CurrentSchema), comment.Text, prefix, postfix, indent, addNewline);
        }

        public override string[] GetSQL(Sequence sequence, string prefix, string postfix, int indent, bool addNewline, bool skipOwned, bool ignoreOwned)
        {
            if (sequence == null)
            {
                return NoSQL;
            }
            if (skipOwned && !string.IsNullOrEmpty(sequence.OwnedColumn))
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);

            buf.Append(spc);
            buf.Append(prefix);
            buf.Append("create sequence ");
            buf.Append(sequence.EscapedIdentifier(CurrentSchema));
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
            if (!ignoreOwned && !string.IsNullOrEmpty(sequence.OwnedColumn))
            {
                buf.Append(" owned by ");
                buf.Append(GetEscapedIdentifier(sequence.OwnedSchemaName, sequence.OwnedTableName, CurrentSchema));
                buf.Append('.');
                buf.Append(GetEscapedIdentifier(sequence.OwnedColumn));
            }
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
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
        public override string[] GetSQL(Parameter p)
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
            return new string[] { buf.ToString() };
        }

        public override string[] GetSQL(StoredFunction function, string prefix, string postfix, int indent, bool addNewline)
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
                buf.Append(_Expand(GetSQL(p)));
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
            return new string[] { buf.ToString() };
        }
        //public override string[] GetSQL(StoredProcedure procedure, string prefix, string postfix, int indent, bool addNewline)
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
            return GetSQL(after, string.Empty, ";", 0, false);
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
                StringBuilder buf = new StringBuilder();
                buf.Append("alter table ");
                buf.Append(tbl);
                buf.Append(" add ");
                buf.Append(_Expand(ctx.GetSQL(after, string.Empty, string.Empty, 0, false)));
                return new string[] { buf.ToString() };
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
                return GetCommentSQL(before.GetCommentType(), before.EscapedIdentifier(CurrentSchema), null, string.Empty, string.Empty, 0, false);
            }
            return GetSQL(after, string.Empty, string.Empty, 0, false);
        }

        public override string[] GetSQL(ComplexType type, string prefix, string postfix, int indent, bool addNewline)
        {
            if (type == null)
            {
                return NoSQL;
            }
            if (type.Columns.Count == 0)
            {
                return NoSQL;
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
                buf.Append(_Expand(GetSQL(type.Columns[i], string.Empty, ",", indent + 2, true)));
            }
            buf.Append(_Expand(GetSQL(type.Columns[n], string.Empty, string.Empty, indent + 2, true)));
            buf.Append(spc);
            buf.Append(")");
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }
        public override string[] GetSQL(PgsqlEnumType type, string prefix, string postfix, int indent, bool addNewline)
        {
            return new string[0];
        }
        public override string[] GetSQL(PgsqlRangeType type, string prefix, string postfix, int indent, bool addNewline)
        {
            return new string[0];
        }
        public override string[] GetSQL(PgsqlBasicType type, string prefix, string postfix, int indent, bool addNewline)
        {
            if (type == null)
            {
                return NoSQL;
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
            buf.AppendLine(",");
            buf.Append(spc);
            buf.Append("  internallength = ");
            if (type.InternalLength == -1)
            {
                buf.Append("variable");
            }
            else
            {
                buf.Append(type.InternalLength);
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
            return new string[] { buf.ToString() };
        }

        public override string[] GetSQL(Tablespace tablespace, string prefix, string postfix, int indent, bool addNewline)
        {
            PgsqlTablespace ts = tablespace as PgsqlTablespace;
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.Append("create tablespace ");
            buf.Append(GetEscapedIdentifier(Name));
            if (ts != null && string.Compare(ts.Owner, ConnectionInfo.UserName, true) != 0)
            {
                buf.Append(" owner ");
                buf.Append(ts.Owner);
            }
            buf.Append(" location ");
            buf.Append(ToLiteralStr(tablespace.Path));
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }
        public override string[] GetSQL(User user, string prefix, string postfix, int indent, bool addNewline)
        {
            PgsqlUser u = user as PgsqlUser;
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.Append("create role ");
            buf.Append(GetEscapedIdentifier(user.Id));
            StringBuilder bufOpt = new StringBuilder();
            if (u != null)
            {
                if (u.IsSuperUser)
                {
                    bufOpt.Append(" superuser");
                }
                if (u.CanCreateDb)
                {
                    bufOpt.Append(" createdb");
                }
                if (u.CanCreateRole)
                {
                    bufOpt.Append(" createrole");
                }
                if (!u.IsInherit)
                {
                    bufOpt.Append(" noinherit");
                }
                if (u.CanLogin)
                {
                    bufOpt.Append(" login");
                }
                if (u.Replication)
                {
                    bufOpt.Append(" replication");
                }
                if (u.BypassRowLevelSecurity)
                {
                    bufOpt.Append(" bypassrls");
                }
                bufOpt.Append(" connection limit ");
                bufOpt.Append(u.ConnectionLimit);
                if (!u.IsPasswordShadowed)
                {
                    bufOpt.Append(" password ");
                    if (u.Password == null)
                    {
                        bufOpt.Append("null");
                    }
                    else
                    {
                        bufOpt.Append(ToLiteralStr(u.Password));
                    }
                }
                bufOpt.Append(" password ");
                //buf.Append(ToLiteralStr(u.Password));
                if (u.PasswordExpiration != DateTime.MaxValue)
                {
                    bufOpt.AppendFormat(" valid until {0:'YYYY-MM-DD'}", u.PasswordExpiration);
                }
                // [ENCRYPTED] PASSWORD 'password' | PASSWORD NULL
                // IN ROLE role_name[, ...]
                // ROLE role_name[, ...]
                // ADMIN role_name[, ...]
            }
            if (0 < bufOpt.Length)
            {
                buf.Append(" with");
                buf.Append(bufOpt.ToString());
            }
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }

        public override string[] GetAlterSQL(Tablespace after, Tablespace before, string prefix, string postfix, int indent, bool addNewline)
        {
            PgsqlTablespace tsA = after as PgsqlTablespace;
            PgsqlTablespace tsB = before as PgsqlTablespace;
            if (after == null)
            {
                throw new ArgumentNullException("after");
            }
            if (before == null)
            {
                throw new ArgumentNullException("before");
            }
            List<string> l = new List<string>();
            if (after.Path != before.Path)
            {
                l.AddRange(GetDropSQL(before, prefix, postfix, indent, false, addNewline));
                l.AddRange(GetSQL(after, prefix, postfix, indent, addNewline));
            }
            string spc = new string(' ', indent);
            string nl = addNewline ? Environment.NewLine : string.Empty;
            if (after.Name != before.Name)
            {
                l.Add(string.Format("{0}{1}alter tablespace {2} rename to {3}{4}{5}", spc, prefix, before.Name, after.Name, postfix, nl));
            }
            if (tsA.Owner != tsB.Owner)
            {
                l.Add(string.Format("{0}{1}alter tablespace {2} owner to {3}{4}{5}", spc, prefix, after.Name, tsA.Owner, postfix, nl));
            }
            // オプション類の変更SQL未実装
            throw new NotImplementedException();
            //return l.ToArray();
        }
        private static bool NeedsDropAndCreate(Trigger after, Trigger before)
        {
            if (after == null)
            {
                throw new ArgumentNullException("after");
            }
            if (before == null)
            {
                throw new ArgumentNullException("before");
            }
            if (after.Timing != before.Timing)
            {
                return true;
            }
            if (after.Event != before.Event)
            {
                return true;
            }
            if (after.EventText != before.EventText)
            {
                return true;
            }
            if (!after.UpdateEventColumns.Equals(before.UpdateEventColumns))
            {
                return true;
            }
            if (after.Table != before.Table)
            {
                return true;
            }
            if (after.Procedure != before.Procedure)
            {
                return true;
            }
            if (after.OrientationText != before.OrientationText)
            {
                return true;
            }
            if (after.ReferencedTableName != before.ReferencedTableName)
            {
                return true;
            }
            if (after.ReferenceNewTable != before.ReferenceNewTable)
            {
                return true;
            }
            if (after.ReferenceOldTable != before.ReferenceOldTable)
            {
                return true;
            }
            if (after.Condition != before.Condition)
            {
                return true;
            }
            if (after.ReferenceNewRow != before.ReferenceNewRow)
            {
                return true;
            }
            if (after.ReferenceOldRow != before.ReferenceOldRow)
            {
                return true;
            }
            if (after.Definition != before.Definition)
            {
                return true;
            }
            return false;
        }
        public override string[] GetAlterSQL(Trigger after, Trigger before, string prefix, string postfix, int indent, bool addNewline)
        {
            if (after == null)
            {
                throw new ArgumentNullException("after");
            }
            if (before == null)
            {
                throw new ArgumentNullException("before");
            }
            List<string> l = new List<string>();
            string spc = new string(' ', indent);
            string nl = addNewline ? Environment.NewLine : string.Empty;
            if (NeedsDropAndCreate(after, before))
            {
                List<string> ret = new List<string>();
                ret.AddRange(GetDropSQL(before, prefix, postfix, indent, true, addNewline));
                ret.AddRange(GetSQL(after, prefix, postfix, indent, addNewline));
                return ret.ToArray();
            }
            if (after.Name != before.Name)
            {
                l.Add(string.Format("{0}{1}alter tablespace {2} on {3} rename to {4}{5}{6}", spc, prefix,
                    before.Name, before.Table.EscapedIdentifier(CurrentSchema), after.Name, postfix, nl));
            }
            return new string[0];
        }
        private static void AddAlterOption(StringBuilder buf, bool after, bool before, string trueValue, string falseValue)
        {
            if (after == before)
            {
                return;
            }
            buf.Append(" ");
            buf.Append(after ? trueValue : falseValue);
        }
        public override string[] GetAlterSQL(User after, User before, string prefix, string postfix, int indent, bool addNewline)
        {
            PgsqlUser uA = after as PgsqlUser;
            PgsqlUser uB = before as PgsqlUser;
            if (after == null)
            {
                throw new ArgumentNullException("after");
            }
            if (before == null)
            {
                throw new ArgumentNullException("before");
            }
            List<string> l = new List<string>();
            string spc = new string(' ', indent);
            string nl = addNewline ? Environment.NewLine : string.Empty;
            if (after.Id != before.Id)
            {
                l.Add(string.Format("{0}{1}alter role {2} rename to {3}{4}{5}", spc, prefix, before.Id, after.Id, postfix, nl));
            }
            StringBuilder bufOpt = new StringBuilder();
            AddAlterOption(bufOpt, uA.IsSuperUser, uB.IsSuperUser, "superuser", "nosuperuser");
            AddAlterOption(bufOpt, uA.CanCreateDb, uB.CanCreateDb, "createdb", "nocreatedb");
            AddAlterOption(bufOpt, uA.CanCreateRole, uB.CanCreateRole, "createrole", "nocreaterole");
            AddAlterOption(bufOpt, uA.IsInherit, uB.IsInherit, "inherit", "noinherit");
            AddAlterOption(bufOpt, uA.CanLogin, uB.CanLogin, "login", "nologin");
            AddAlterOption(bufOpt, uA.Replication, uB.Replication, "replication", "noreplication");
            AddAlterOption(bufOpt, uA.BypassRowLevelSecurity, uB.BypassRowLevelSecurity, "bypassrls", "nobypassrls");
            if (uA.ConnectionLimit != uB.ConnectionLimit)
            {
                bufOpt.Append(" connection limit ");
                bufOpt.Append(uA.ConnectionLimit);
            }
            if (!uA.IsPasswordShadowed && (uB.IsPasswordShadowed || uA.Password != uB.Password))
            {
                bufOpt.Append(" password ");
                bufOpt.Append(uA.Password == null ? "null" : ToLiteralStr(uA.Password));
            }
            if (uA.PasswordExpiration != uB.PasswordExpiration)
            {
                bufOpt.Append(" valid until");
                bufOpt.Append(uA.PasswordExpiration == DateTime.MaxValue ? "infinity" : uA.PasswordExpiration.ToString("'yyyy-MM-dd'"));
            }
            if (0 < bufOpt.Length)
            {
                l.Add(string.Format("{0}{1}alter role {2} with {3}{4}{5}", spc, prefix, after.Id, bufOpt.ToString(), postfix, nl));
            }
            return l.ToArray();
        }


        private string[] GetDropSQLInternal(SchemaObject target, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            if (target == null)
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(prefix);
            buf.Append("drop ");
            buf.Append(target.GetSqlType().ToLower());
            buf.Append(" ");
            buf.Append(target.EscapedIdentifier(CurrentSchema));
            if (cascade)
            {
                buf.Append(" cascade");
            }
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }
        public override string[] GetDropSQL(SchemaObject table, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            throw new NotImplementedException();
        }
        public override string[] GetDropSQL(Table table, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            return GetDropSQLInternal(table, prefix, postfix, indent, cascade, addNewline);
        }
        public override string[] GetDropSQL(View table, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            return GetDropSQLInternal(table, prefix, postfix, indent, cascade, addNewline);
        }
        public override string[] GetDropSQL(Column column, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            if (column == null)
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(prefix);
            buf.Append("alter table ");
            buf.Append(column.Table.EscapedIdentifier(CurrentSchema));
            buf.Append(" drop column ");
            buf.Append(column.EscapedName);
            if (cascade)
            {
                buf.Append(" cascade");
            }
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }

        public override string[] GetDropSQL(Comment comment, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            if (comment == null)
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.AppendFormat("comment on {0} {1} is null", comment.GetCommentType().ToLower(), comment.EscapedIdentifier(CurrentSchema));
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }

        public override string[] GetDropSQL(Constraint constraint, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            if (constraint == null)
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.AppendFormat("alter table {0} drop constraint {1}", constraint.Table.EscapedIdentifier(CurrentSchema), GetEscapedIdentifier(constraint.Name));
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }
        public override string[] GetDropSQL(Trigger trigger, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            if (trigger == null)
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.AppendFormat("drop trigger {0} on {1}", trigger.EscapedIdentifier(trigger.Table.SchemaName), trigger.Table.EscapedIdentifier(CurrentSchema));
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }
        public override string[] GetDropSQL(Index index, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            return GetDropSQLInternal(index, prefix, postfix, indent, cascade, addNewline);
        }
        public override string[] GetDropSQL(Sequence sequence, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            return GetDropSQLInternal(sequence, prefix, postfix, indent, cascade, addNewline);
        }
        private string GetSQLIdentifier(StoredFunction function, string baseSchemaName)
        {
            StringBuilder buf = new StringBuilder();
            buf.Append(function.EscapedIdentifier(baseSchemaName));
            buf.Append('(');
            bool needComma = false;
            foreach (Parameter p in function.Parameters)
            {
                if (p.Direction != ParameterDirection.Input && p.Direction != ParameterDirection.InputOutput)
                {
                    continue;
                }
                if (needComma)
                {
                    buf.Append(',');
                }
                string s = p.DirectionStr;
                if (!string.IsNullOrEmpty(s))
                {
                    buf.Append(s);
                    buf.Append(' ');
                }
                buf.Append(p.DataType);
                needComma = true;
            }
            buf.Append(')');
            return buf.ToString();
        }

        public override string[] GetDropSQL(StoredFunction function, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            if (function == null)
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            string spc = new string(' ', indent);
            buf.Append(spc);
            buf.Append(prefix);
            buf.Append("drop function ");
            buf.Append(GetSQLIdentifier(function, CurrentSchema));
            if (cascade)
            {
                buf.Append(" cascade");
            }
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }
        //public override string GetDropSQL(StoredProcedure procedure, string prefix, string postfix, int indent, bool cascade, bool addNewline);
        public override string[] GetDropSQL(ComplexType type, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            return GetDropSQLInternal(type, prefix, postfix, indent, cascade, addNewline);
        }
        public override string[] GetDropSQL(PgsqlEnumType type, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            return GetDropSQLInternal(type, prefix, postfix, indent, cascade, addNewline);
        }
        public override string[] GetDropSQL(PgsqlRangeType type, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            return GetDropSQLInternal(type, prefix, postfix, indent, cascade, addNewline);
        }
        public override string[] GetDropSQL(PgsqlBasicType type, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            return GetDropSQLInternal(type, prefix, postfix, indent, cascade, addNewline);
        }
        public override string[] GetDropSQL(Tablespace tablespace, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            if (tablespace == null)
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(prefix);
            buf.Append("drop tablespace if exists ");
            buf.Append(GetEscapedIdentifier(tablespace.Name));
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }
        public override string[] GetDropSQL(User user, string prefix, string postfix, int indent, bool cascade, bool addNewline)
        {
            if (user == null)
            {
                return NoSQL;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(prefix);
            buf.Append("drop user if exists ");
            buf.Append(GetEscapedIdentifier(user.Name));
            buf.Append(postfix);
            if (addNewline)
            {
                buf.AppendLine();
            }
            return new string[] { buf.ToString() };
        }

        private string _serverEncoding;
        private string _clientEncoding;
        private string[] _encodings;
        private string GetStringFromSQL(string sql, NpgsqlConnection connection)
        {
            using (NpgsqlCommand cmd = new NpgsqlCommand(sql, connection))
            {
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetString(0);
                    }
                }
            }
            return null;
        }
        protected override void LoadEncodings(IDbConnection connection)
        {
            NpgsqlConnection conn = connection as NpgsqlConnection;
            _clientEncoding = GetStringFromSQL(DataSet.Properties.Resources.ClientEncoding_SQL, conn);
            _serverEncoding = GetStringFromSQL(DataSet.Properties.Resources.ServerEncoding_SQL, conn);
            using (NpgsqlCommand cmd = new NpgsqlCommand(DataSet.Properties.Resources.GetEncodings_SQL, conn))
            {
                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                {
                    List<string> l = new List<string>();
                    while (reader.Read())
                    {
                        l.Add(reader.GetString(0));
                    }
                    _encodings = l.ToArray();
                }
            }
        }
        public override string GetServerEncoding()
        {
            return _serverEncoding;
        }
        public override string GetClientEncoding()
        {
            return _clientEncoding;
        }
        public override string[] GetEncodings()
        {
            return _encodings;
        }

    }
}
