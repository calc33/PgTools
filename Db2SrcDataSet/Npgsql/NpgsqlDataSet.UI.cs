using System;
using System.Collections.Generic;
using System.Text;

namespace Db2Source
{
    partial class NpgsqlDataSet
    {
        public override TreeNode[] GetVisualTree()
        {
            List<TreeNode> top = new List<TreeNode>();
            List<Schema> l = new List<Schema>(Schemas);
            l.Sort();
            foreach (Schema sc in l)
            {
                TreeNode nodeSc = new TreeNode(sc.Name, null, typeof(Schema), 0, (sc.Name == CurrentSchema), sc.IsHidden)
                {
                    Target = sc
                };
                top.Add(nodeSc);
                List<TreeNode> lSc = new List<TreeNode>();
                List<SchemaObject> types = new List<SchemaObject>();
                List<Table> tbls = new List<Table>();
                List<View> views = new List<View>();
                List<StoredFunction> funcs = new List<StoredFunction>();
                List<StoredFunction> trFuncs = new List<StoredFunction>();
                List<Sequence> seqs = new List<Sequence>();
                foreach (SchemaObject obj in sc.Objects)
                {
                    if (obj is Table)
                    {
                        tbls.Add((Table)obj);
                    }
                    if (obj is View)
                    {
                        views.Add((View)obj);
                    }
                    if (obj is StoredFunction)
                    {
                        StoredFunction fn = (StoredFunction)obj;
                        if (string.Compare(fn.DataType, "trigger") == 0)
                        {
                            trFuncs.Add(fn);
                        }
                        else
                        {
                            funcs.Add(fn);
                        }
                    }
                    if (obj is Sequence)
                    {
                        seqs.Add((Sequence)obj);
                    }
                    if (obj is Type_ || obj is ComplexType)
                    {
                        types.Add(obj);
                    }
                }
                tbls.Sort();
                views.Sort();
                seqs.Sort();
                types.Sort();
                TreeNode nodeGrp;
                List<TreeNode> lGrp;

                lGrp = new List<TreeNode>();
                nodeGrp = new TreeNode("表", "表 ({0})", typeof(Table), 0, false, false);
                lSc.Add(nodeGrp);
                foreach (Table t in tbls)
                {
                    lGrp.Add(new TreeNode(t));
                }
                nodeGrp.Children = lGrp.ToArray();

                lGrp = new List<TreeNode>();
                nodeGrp = new TreeNode("ビュー", "ビュー ({0})", typeof(View), 0, false, false);
                lSc.Add(nodeGrp);
                foreach (View t in views)
                {
                    lGrp.Add(new TreeNode(t));
                }
                nodeGrp.Children = lGrp.ToArray();

                lGrp = new List<TreeNode>();
                nodeGrp = new TreeNode("型", "型 ({0})", typeof(Type_), 0, false, false);
                lSc.Add(nodeGrp);
                foreach (SchemaObject t in types)
                {
                    lGrp.Add(new TreeNode(t));
                }
                nodeGrp.Children = lGrp.ToArray();

                lGrp = new List<TreeNode>();
                nodeGrp = new TreeNode("ストアド関数", "ストアド関数 ({0})", typeof(View), 0, false, false);
                lSc.Add(nodeGrp);
                foreach (StoredFunction fn in funcs)
                {
                    lGrp.Add(new TreeNode(fn));
                }
                nodeGrp.Children = lGrp.ToArray();

                lGrp = new List<TreeNode>();
                nodeGrp = new TreeNode("トリガー関数", "トリガー関数 ({0})", typeof(View), 1, false, false);
                lSc.Add(nodeGrp);
                foreach (StoredFunction fn in trFuncs)
                {
                    lGrp.Add(new TreeNode(fn));
                }
                nodeGrp.Children = lGrp.ToArray();

                lGrp = new List<TreeNode>();
                nodeGrp = new TreeNode("シーケンス", "シーケンス ({0})", typeof(Sequence), 0, false, false);
                lSc.Add(nodeGrp);
                foreach (Sequence t in seqs)
                {
                    lGrp.Add(new TreeNode(t));
                }
                nodeGrp.Children = lGrp.ToArray();

                nodeSc.Children = lSc.ToArray();
            }
            List<TreeNode> lDb = new List<TreeNode>();
            TreeNode nodeDb = new TreeNode(Database.Name, Database.Name, typeof(Database), 0, true, false);
            nodeDb.Target = Database;
            nodeDb.Children = top.ToArray();
            lDb.Add(nodeDb);
            TreeNode nodeOtherDbs = new TreeNode("その他のデータベース", "その他のデータベース{0}", typeof(Database), 0, false, false);
            List<TreeNode> lOtherDb = new List<TreeNode>();
            foreach (Database db in OtherDatabases)
            {
                nodeDb = new TreeNode(db.Name, db.Name, typeof(Database), 0, false, false);
                nodeDb.Target = db;
                lOtherDb.Add(nodeDb);
            }
            nodeOtherDbs.Children = lOtherDb.ToArray();
            lDb.Add(nodeOtherDbs);
            return lDb.ToArray();
        }
        public override Tuple<int, int> GetErrorPosition(Exception t, string sql, int offset)
        {
            if (!(t is Npgsql.PostgresException))
            {
                return null;
            }
            Npgsql.PostgresException ex = (Npgsql.PostgresException)t;
            int p0 = ex.Position;
            if (p0 <= 0)
            {
                return null;
            }
            p0--;
            TokenizedPgsql tsql = new TokenizedPgsql(sql.Substring(offset));
            bool wasColon = false;
            int seq = 1;
            Dictionary<string, int> pdict = new Dictionary<string, int>();
            foreach (PgsqlToken token in tsql.Tokens)
            {
                if (wasColon && token.Kind == TokenKind.Identifier)
                {
                    // パラメータは内部的に数字に置換して実行し、
                    // 置換後のSQLでの文字位置が返るため
                    // そのままでは位置がずれてしまう
                    int idx;
                    if (!pdict.TryGetValue(token.Value, out idx))
                    {
                        idx = seq++;
                        pdict.Add(token.Value, idx);
                    }
                    p0 += (token.Value.Length - idx.ToString().Length);
                }
                wasColon = (token.ID == TokenID.Colon);
            }
            int n = sql.Length;
            int p;
            for (p = p0; p < n && !char.IsWhiteSpace(sql, p); p++) ;
            if (p0 < offset)
            {
                return null;
            }
            return new Tuple<int, int>(p0 - offset, p - p0);
        }
        public override Tuple<int, int> GetWordAt(string sql, int position)
        {
            TokenizedPgsql tsql = new TokenizedPgsql(sql, position);
            PgsqlToken sel = (PgsqlToken)tsql.Selected;
            if (sel == null)
            {
                return null;
            }
            return new Tuple<int, int>(sel.StartPos, sel.EndPos - sel.StartPos + 1);
        }
        private static string GetExceptionMessage(Npgsql.PostgresException t)
        {
            StringBuilder buf = new StringBuilder();
            buf.Append(t.Message);
            if (!string.IsNullOrEmpty(t.Detail))
            {
                buf.AppendLine();
                buf.Append(t.Detail);
            }
            if (!string.IsNullOrEmpty(t.Hint))
            {
                buf.AppendLine();
                buf.Append(t.Hint);
            }
            if (!string.IsNullOrEmpty(t.Where))
            {
                buf.AppendLine();
                buf.AppendLine();
                buf.Append(t.Where);
            }
            return buf.ToString();
        }
        /// <summary>
        /// 例外のダイアログ出力用メッセージを取得
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public override string GetExceptionMessage(Exception t)
        {
            if (t is Npgsql.PostgresException)
            {
                return GetExceptionMessage((Npgsql.PostgresException)t);
            }
            return t.Message;
        }

        public override bool SuggestsDropCascade(Exception t)
        {
            Npgsql.NpgsqlException ex = t as Npgsql.PostgresException;
            if (ex == null)
            {
                return false;
            }
            string code = (string)ex.Data["Code"];
            return code == "2BP01";
        }
        public override bool AllowOutputParameter
        {
            get
            {
                return false;
            }
        }
    }
}
