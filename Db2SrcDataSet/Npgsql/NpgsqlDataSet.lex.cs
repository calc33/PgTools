using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    partial class NpgsqlDataSet
    {
        public enum TokenID: int
        {
            Identifier = 0x41,  // A
            Numeric = 0x30,     // 0
            Space = 0x20,       // SPC
            Comment = 0x2f2a,   // /*
            Factorial = 0x21,   // ! 階乗
            //DoubleQuote = 0x22,// "
            BinaryXor = 0x23,   // #
            DefBody = 0x24,     // $
            Percent = 0x25,     // %
            BinaryAnd = 0x26,   // &
            Literal = 0x27,     // '
            ParenthesesL = 0x28,// (
            ParenthesesR = 0x29,// )
            Asterisk = 0x2a,    // *
            Plus = 0x2b,        // +
            Comma = 0x2c,       // ,
            Minus = 0x2d,       // -
            Period = 0x2e,      // .
            Slash = 0x2f,       // /
            Colon = 0x3a,       // :
            Semicolon = 0x3b,   // ;
            Less = 0x3c,        // <
            Equal = 0x3d,       // =
            Greater = 0x3e,     // >
            Question = 0x3f,    // ?
            Abs = 0x40,         // @
            BracketL = 0x5b,    // [
            Escape = 0x5c,      // \
            BracketR = 0x5d,    // ]
            Accent = 0x5e,      // ^
            Underline = 0x5f,   // _
            BraceL = 0x7b,      // {
            BinaryOr = 0x7c,    // |
            BraceR = 0x7d,      // }
            BinaryNot = 0x7e,   // ~
            LessEq = 0x3c3d,        // <=
            GreaterEq = 0x3e3d,     // >=
            ShiftL = 0x3c3c,        // <<
            ShiftR = 0x3e3e,        // >>
            ContainsL = 0x3c3c3d,    // <<=
            ContainsR = 0x3e3e3d,    // >>=
            Contains = 0x2626,      // &&
            FactorialPre = 0x2121,  // !!
            Join = 0x7c7c,          // ||
            Sqrt = 0x7c2f,          // |/
            Cbrt = 0x7c7c2f,        // ||/
            ReMatch = 0x7e,         // ~
            ReMatchNC = 0x7e2a,     // ~*
            ReUnmatch = 0x217e,     // !~
            ReUnmatchNC = 0x217e2a, // !~*
            Like = 0x7e7e,          // ~~
            LikeNC = 0x7e7e2a,      // ~~*
            NotLike = 0x217e7e,     // !~~
            NotLikeNC = 0x217e7e2a, // !~~*
            GeoLen = 0x402d40,      // @-@
            GeoCenter = 0x4040,     // @@
            GeoCross = 0x2626,      // &&
            GeoNotRight = 0x263c,   // &<
            GeoNotLeft = 0x263e,    // &>
            GeoLower = 0x3c3c7c,    // <<|
            GeoUpper = 0x7c3e3e,    // |>>
            GeoNotUpper = 0x263c7c, // &<|
            GeoNotLower = 0x7c263e, // |&>
            GeoOp1 = 0x3c5e,        // <^
            GeoOp2 = 0x3e5e,        // >^
            GeoOp3 = 0x3f2d,        // ?-
            GeoOp4 = 0x3f7c,        // ?|
            GeoOp5 = 0x3f2d7c,      // ?-|
            GeoOp6 = 0x3f7c7c,      // ?||
            GeoOp7 = 0x403e,        // @>
            GeoOp8 = 0x3c40,        // <@
            GeoOp9 = 0x7e3d,        // ~=
            SearchOp1 = 0x4040,     // @@
            SearchOp2 = 0x404040,   // @@@
            JsonOp1 = 0x2d3e,       // ->
            JsonOp2 = 0x2d3e3e,     // ->>
            JsonOp3 = 0x233e,       // #>
            JsonOp4 = 0x233e3e,     // #>>
            JsonOp5 = 0x3f26,       // ?&
            RangeOp1 = 0x2d7c2d,    // -|-
            // Virtual TokenID
            Expr = 0x45585052,      // Expression(EXPR)
            Stmt = 0x53544d54,      // Statement(STMT)
        }
        public class PgsqlToken: Token
        {
            public new TokenID ID
            {
                get
                {
                    return (TokenID)base.ID;
                }
                set
                {
                    base.ID = (int)value;
                }
            }
            internal PgsqlToken(TokenizedPgsql owner, TokenKind kind, TokenID identifier, int start, int current) : base(owner, kind, (int)identifier, start, current)
            {
                IsReservedWord = (Kind == TokenKind.Identifier) && IsReservedWord(Value);
            }

            internal PgsqlToken(TokenizedPgsql owner, TokenKind kind, char identifier, int start, int current)
                :base(owner, kind, start, current)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(new char[] { identifier }, 0, 1);
                int v = 0;
                foreach (byte b in bytes)
                {
                    v = v << 8 | b;
                }
                ID = (TokenID)v;
                IsReservedWord = (Kind == TokenKind.Identifier) && IsReservedWord(Value);
            }

            internal PgsqlToken(TokenizedPgsql owner, TokenKind kind, char[] ids, int start, int current)
                : base(owner, kind, start, current)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(ids);
                int v = 0;
                foreach (byte b in bytes)
                {
                    v = v << 8 | b;
                }
                ID = (TokenID)v;
                IsReservedWord = (Kind == TokenKind.Identifier) && IsReservedWord(Value);
            }

            internal PgsqlToken(TokenizedPgsql owner, TokenKind kind, string ids, int start, int current)
                : base(owner, kind, start, current)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(ids);
                int v = 0;
                foreach (byte b in bytes)
                {
                    v = v << 8 | b;
                }
                ID = (TokenID)v;
                IsReservedWord = (Kind == TokenKind.Identifier) && IsReservedWord(Value);
            }

            private string TokenIdStr()
            {
                int n = 4;
                byte[] b = new byte[n];
                uint v = (uint)ID;
                int i;
                for (i = n - 1; 0 <= i; i--)
                {
                    b[i] = (byte)(v % 256);
                    v >>= 8;
                    if (v == 0)
                    {
                        break;
                    }
                }
                return Encoding.ASCII.GetString(b, i, n - i);
            }

            public override string ToString()
            {
                return string.Format("({0}:{1})\"{2}\"", Kind, TokenIdStr(), Value);
            }
        }

        public class TokenizedPgsql: TokenizedSQL
        {
            public string Extract(PgsqlToken startToken, PgsqlToken endToken)
            {
                return Sql.Substring(startToken.StartPos, endToken.EndPos - startToken.StartPos + 1);
            }

            private static Dictionary<char, bool> InitIdentifierEndChars()
            {
                Dictionary<char, bool> dict = new Dictionary<char, bool>();
                foreach (char c in " !\"#$%&'()*+,-./:;<=>?@[\\]^`{|}~")
                {
                    dict.Add(c, true);
                }
                return dict;
            }
            internal static readonly Dictionary<char, bool> IdentifierEndChars = InitIdentifierEndChars();
            
            private char SqlCh(int index)
            {
                if (index < 0 || Sql.Length <= index)
                {
                    return '\0';
                }
                return Sql[index];
            }

            private PgsqlToken TryGetNextToken(string sql, TokenKind kind, string token, ref int position)
            {
                if (sql.Length < position + token.Length)
                {
                    return null;
                }
                int p = position;
                for (int i = 0; i < token.Length; i++, p++)
                {
                    if (sql[p] != token[i])
                    {
                        return null;
                    }
                }
                position = p;
                return new PgsqlToken(this, kind, token, position, p);
            }

            /// <summary>
            /// positionで指定された位置から字句解析をして
            /// 先頭のTokenを返す
            /// </summary>
            /// <param name="position"></param>
            internal PgsqlToken GetNextToken(ref int p)
            {
                List<PgsqlToken> tokens = new List<PgsqlToken>();
                int n = Sql.Length;
                PgsqlToken token;
                while (p < n)
                {
                    int p0 = p;
                    bool isNewLine = false;
                    while (p < n && char.IsWhiteSpace(Sql, p))
                    {
                        char sp = Sql[p];
                        if (sp == '\u0010' || sp == '\u0013')
                        {
                            isNewLine = true;
                        }
                        p++;
                    }
                    if (p0 < p)
                    {
                        return new PgsqlToken(this, isNewLine ? TokenKind.NewLine : TokenKind.Space, TokenID.Space, p0, p);
                    }
                    if (!(p < n))
                    {
                        return null;
                    }
                    p0 = p;
                    char c = Sql[p];
                    switch (c)
                    {
                        case '"':
                            while (p < n && Sql[p] == '"')
                            {
                                for (p++; p < n && Sql[p] != '"'; p++)
                                {
                                    if (char.IsSurrogate(Sql[p]))
                                    {
                                        p++;
                                    }
                                }
                                if (p < n)
                                {
                                    p++;
                                }
                            }
                            return new PgsqlToken(this, TokenKind.Identifier, TokenID.Identifier, p0, p);
                        case '\'':
                            while (p < n && Sql[p] == '\'')
                            {
                                for (p++; p < n && Sql[p] != '\''; p++)
                                {
                                    if (char.IsSurrogate(Sql[p]))
                                    {
                                        p++;
                                    }
                                }
                                if (p < n)
                                {
                                    p++;
                                }
                            }
                            return new PgsqlToken(this, TokenKind.Literal, TokenID.Literal, p0, p);
                        case '(':
                        case ')':
                        case '*':
                        case '+':
                        case ',':
                        case '.':
                        case '=':
                        case '[':
                        case ']':
                        case '#':
                        case '%':
                        case '&':
                            p++;
                            return new PgsqlToken(this, TokenKind.Operator, c, p0, p);
                        case ':':
                            token = TryGetNextToken(Sql, TokenKind.Operator, "::", ref p);
                            if (token != null)
                            {
                                return token;
                            }
                            p++;
                            return new PgsqlToken(this, TokenKind.Operator, c, p0, p);
                        case ';':
                            p++;
                            return new PgsqlToken(this, TokenKind.Semicolon, c, p0, p);
                        case '<':
                            token = TryGetNextToken(Sql, TokenKind.Operator, "<=", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, "<<", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, "<>", ref p);
                            if (token != null)
                            {
                                return token;
                            }
                            p++;
                            return new PgsqlToken(this, TokenKind.Operator, c, p0, p);
                        case '>':
                            token = TryGetNextToken(Sql, TokenKind.Operator, ">=", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, ">>", ref p);
                            if (token != null)
                            {
                                return token;
                            }
                            p++;
                            return new PgsqlToken(this, TokenKind.Operator, c, p0, p);
                        case '~':
                            token = TryGetNextToken(Sql, TokenKind.Operator, "~*", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, "~~*", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, "~~", ref p);
                            if (token != null)
                            {
                                return token;
                            }
                            p++;
                            return new PgsqlToken(this, TokenKind.Operator, c, p0, p);
                        case '/':
                            if (SqlCh(p + 1) == '*')
                            {
                                p++;
                                while (p < n)
                                {
                                    for (p++; p < n && Sql[p] != '*'; p++) ;
                                    if (SqlCh(p + 1) == '/')
                                    {
                                        p += 2;
                                        break;
                                    }
                                }
                                return new PgsqlToken(this, TokenKind.Comment, TokenID.Comment, p0, p);
                            }
                            p++;
                            return new PgsqlToken(this, TokenKind.Operator, c, p0, p);
                        case '-':
                            if (SqlCh(p + 1) == '-')
                            {
                                for (p += 2; p < n && Sql[p] != '\r' && Sql[p] != '\n'; p++) ;
                                if (p < n - 1 && Sql[p] == '\r' && Sql[p + 1] == '\n')
                                {
                                    p++;
                                }
                                if (p < n)
                                {
                                    p++;
                                }
                                return new PgsqlToken(this, TokenKind.Comment, TokenID.Comment, p0, p);
                            }
                            p++;
                            return new PgsqlToken(this, TokenKind.Operator, c, p0, p);
                        case '$':
                            p0 = p;
                            for (p++; p < n && Sql[p] != '$'; p++) ;
                            string mark = Sql.Substring(p0, p - p0 + 1);
                            while (p < n)
                            {
                                for (p++; p < n && Sql[p] != '$'; p++) ;
                                if (p + mark.Length <= n && string.Compare(Sql.Substring(p, mark.Length), mark, true) == 0)
                                {
                                    p += mark.Length;
                                    break;
                                }
                            }
                            token = new PgsqlToken(this, TokenKind.DefBody, TokenID.DefBody, p0, p);
                            string[] defBody = ExtractDefBody(token.GetValue());
                            token.Child = new TokenizedPgsql(defBody[1]);
                            return token;
                        case '!':
                            token = TryGetNextToken(Sql, TokenKind.Operator, "!=", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, "!!", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, "!~*", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, "!~~*", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, "!~~", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, "!~", ref p);
                            if (token != null)
                            {
                                return token;
                            }
                            p++;
                            return new PgsqlToken(this, TokenKind.Operator, c, p0, p);
                        case '|':
                            token = TryGetNextToken(Sql, TokenKind.Operator, "||/", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, "||", ref p)
                                ?? TryGetNextToken(Sql, TokenKind.Operator, "|/", ref p);
                            if (token != null)
                            {
                                return token;
                            }
                            p++;
                            return new PgsqlToken(this, TokenKind.Operator, c, p0, p);
                        case '?':
                        case '@':
                        case '\\':
                        case '^':
                        //case '_':
                        case '`':
                        case '{':
                        case '}':
                            p++;
                            return new PgsqlToken(this, TokenKind.Operator, c, p0, p);
                        default:
                            if (char.IsDigit(Sql, p))
                            {
                                for (p++; p < n && (char.IsNumber(Sql, p) || Sql[p] == '.'); p++) ;
                                return new PgsqlToken(this, TokenKind.Numeric, '0', p0, p);
                            }
                            for (p++; p < n && !char.IsWhiteSpace(Sql, p) && !TokenizedPgsql.IdentifierEndChars.ContainsKey(Sql[p]); p++)
                            {
                                if (char.IsSurrogate(Sql, p))
                                {
                                    p++;
                                }
                            }
                            return new PgsqlToken(this, TokenKind.Identifier, 'A', p0, p);
                    }
                }
                return null;
            }

            private string[] ExtractDefBody(string sql)
            {
                if (string.IsNullOrEmpty(sql))
                {
                    return StrUtil.EmptyStringArray;
                }
                if (sql[0] != '$' || sql[sql.Length - 1] != '$')
                {
                    return new string[] { sql };
                }
                int p1 = sql.IndexOf('$', 1);
                string mark1 = sql.Substring(0, p1 + 1);
                int p2 = sql.LastIndexOf('$', sql.Length - 2);
                // 最後の改行コードを終端記号に含める(改行コードを残す)
                if (0 < p2)
                {
                    char c = sql[p2 - 1];
                    if (c == '\n')
                    {
                        p2--;
                        if (0 < p2 && sql[p2 - 1] == '\r')
                        {
                            p2--;
                        }
                    }
                    else if (c == '\r')
                    {
                        p2--;
                    }
                }
                string mark2 = sql.Substring(p2);
                string body = sql.Substring(p1 + 1, p2 - p1 - 1);
                return new string[] { mark1, body, mark2 };
            }

            public TokenizedPgsql(string sql) : base(sql) { }

            public TokenizedPgsql(string sql, int offset) : base(sql)
            {
            }

            private class PgsqlTokenEnumerator : IEnumerator<Token>
            {
                TokenizedPgsql _owner;
                private int _start;
                private PgsqlToken _current;
                private int _position;
                public Token Current { get { return _current; } }

                object IEnumerator.Current { get { return _current; } }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    _current = _owner.GetNextToken(ref _position);
                    return _current != null;
                }

                public void Reset()
                {
                    _position = _start;
                    _current = null;
                }
                internal PgsqlTokenEnumerator(TokenizedPgsql owner, int startPosition)
                {
                    _owner = owner;
                    _start = startPosition;
                    _position = _start;
                }
            }
            protected override IEnumerator<Token> GetEnumeratorCore()
            {
                return new PgsqlTokenEnumerator(this, 0);
            }
        }

        private string NormalizeIdentifier(string value, CaseRule rule, bool noQuote)
        {
            string s = DequoteIdentifier(value);
            switch (rule)
            {
                case CaseRule.Lowercase:
                    s = s.ToLower();
                    break;
                case CaseRule.Uppercase:
                    s = s.ToUpper();
                    break;
                default:
                    throw new ArgumentException(string.Format("rule={0}に対する処理がありません", Enum.GetName(typeof(CaseRule), rule)));
            }
            if (!noQuote && NeedQuotedPgsqlIdentifier(s, false))
            {
                s = GetQuotedIdentifier(s);
            }
            return s;
        }
        public override string[] ExtractDefBody(string sql, CaseRule reservedRule, CaseRule identifierRule)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return StrUtil.EmptyStringArray;
            }
            if (sql[0] != '$' || sql[sql.Length - 1] != '$')
            {
                return new string[] { sql };
            }
            int p1 = sql.IndexOf('$', 1);
            string mark1 = sql.Substring(0, p1 + 1);
            int p2 = sql.LastIndexOf('$', sql.Length - 2);
            // 最後の改行コードを終端記号に含める(改行コードを残す)
            if (0 < p2)
            {
                char c = sql[p2 - 1];
                if (c == '\n')
                {
                    p2--;
                    if (0 < p2 && sql[p2 - 1] == '\r')
                    {
                        p2--;
                    }
                }
                else if (c == '\r')
                {
                    p2--;
                }
            }
            string mark2 = sql.Substring(p2);
            string body = sql.Substring(p1 + 1, p2 - p1 - 1);
            return new string[] { mark1, body, mark2 };
        }
        private string NormalizeDefBody(string sql, CaseRule reservedRule, CaseRule identifierRule)
        {
            string[] defs = ExtractDefBody(sql, reservedRule, identifierRule);
            if (defs.Length != 3)
            {
                return sql;
            }
            return defs[0] + NormalizeSQL(defs[1]) + defs[2];
        }

        public override TokenizedSQL Tokenize(string sql)
        {
            return new TokenizedPgsql(sql);
        }

        public override string NormalizeSQL(string sql, CaseRule reservedRule, CaseRule identifierRule)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return sql;
            }
            StringBuilder buf = new StringBuilder();
            TokenizedSQL tsql = Tokenize(sql);
            bool wasLanguage = false;
            bool isSqlDef = false;
            foreach (PgsqlToken t in tsql)
            {
                if (t.Kind == TokenKind.Space || t.Kind == TokenKind.NewLine || t.Kind == TokenKind.Comment)
                {
                    continue;
                }
                if (wasLanguage && t.Kind == TokenKind.Identifier)
                {
                    string lang = t.Value.ToLower();
                    if (lang == "plpgsql" || lang == "sql")
                    {
                        isSqlDef = true;
                        break;
                    }
                }
                wasLanguage = (t.Kind == TokenKind.Identifier && t.Value.ToLower() == "language");
            }
            bool noQuote = false;
            PgsqlToken holdedSpc = null;
            foreach (PgsqlToken t in tsql)
            {
                // 行末の空白を除去
                if (holdedSpc != null && t.Kind != TokenKind.NewLine)
                {
                    buf.Append(holdedSpc.Value);
                }
                holdedSpc = null;
                switch (t.Kind)
                {
                    case TokenKind.Identifier:
                        if (IsReservedWord(t.Value))
                        {
                            buf.Append(NormalizeIdentifier(t.Value, reservedRule, true));
                        }
                        else
                        {
                            buf.Append(NormalizeIdentifier(t.Value, identifierRule, noQuote));
                        }
                        break;
                    case TokenKind.DefBody:
                        if (isSqlDef)
                        {
                            buf.Append(NormalizeDefBody(t.Value, reservedRule, identifierRule));
                        }
                        else
                        {
                            buf.Append(t.Value);
                        }
                        break;
                    case TokenKind.Space:
                        // 行末の空白を除去(次のトークンを見て判断)
                        holdedSpc = t;
                        break;
                    case TokenKind.NewLine:
                        // 改行をCRLFに統一
                        buf.AppendLine();
                        break;
                    default:
                        buf.Append(t.Value);
                        break;
                }
                noQuote = (t.Kind == TokenKind.Operator) && (t.ID == TokenID.Colon);
            }
            return buf.ToString();
        }

        public override SQLParts SplitSQL(string sql)
        {
            TokenizedSQL tsql = Tokenize(sql);
            IEnumerator<Token> enumerator = tsql.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return new SQLParts() { Items = new SQLPart[0], ParameterNames = new string[0] };
            }
            List<SQLPart> lSql = new List<SQLPart>();
            List<string> lParamAll = new List<string>();
            do
            {
                for (Token t = enumerator.Current; t.Kind != TokenKind.Space && t.Kind != TokenKind.NewLine && t.Kind != TokenKind.Comment; t = enumerator.Current)
                {
                    enumerator.MoveNext();
                }
                List<string> lParam = new List<string>();
                Token tStart = enumerator.Current;
                Token tEnd = tStart;
                Token tPrev = null;
                bool endByNewLine = false;
                if (tStart.Kind == TokenKind.Identifier && (string.Compare(tStart.Value, "begin") == 0 || string.Compare(tStart.Value, "start") == 0))
                {
                    endByNewLine = true;
                }
                bool executable = false;
                for (Token t = enumerator.Current; t != null && t.Kind != TokenKind.Semicolon && !(endByNewLine && t.Kind == TokenKind.NewLine); t = enumerator.Current)
                {
                    // パラメータ抽出
                    if (tPrev != null && tPrev.ID == (int)TokenID.Colon && t.Kind == TokenKind.Identifier)
                    {
                        lParam.Add(DequoteIdentifier(t.Value));
                    }
                    tPrev = t;
                    tEnd = t;
                    TokenKind k = t.Kind;
                    if (k != TokenKind.Space && k != TokenKind.Comment && k != TokenKind.NewLine)
                    {
                        executable = true;
                    }
                    enumerator.MoveNext();
                }
                string s = tsql.Extract(tStart, tEnd);
                SQLPart sp = new SQLPart()
                {
                    Offset = tStart.StartPos,
                    SQL = s,
                    ParameterNames = lParam.ToArray(),
                    IsExecutable = executable,
                };
                lSql.Add(sp);
                lParamAll.AddRange(lParam);
            } while (enumerator.Current != null);

            Dictionary<string, bool> dict = new Dictionary<string, bool>();
            List<string> lP = new List<string>();
            foreach (string s in lParamAll)
            {
                if (dict.ContainsKey(s))
                {
                    continue;
                }
                dict.Add(s, true);
                lP.Add(s);
            }

            return new SQLParts()
            {
                Items = lSql.ToArray(),
                ParameterNames = lP.ToArray()
            };
        }
        //public override IDbCommand[] Execute(SQLParts sqls, ref ParameterStoreCollection parameters)
        //{
        //    bool modified;
        //    parameters = ParameterStore.GetParameterStores(sqls.ParameterNames, parameters, out modified);
        //    if (modified)
        //    {
        //        return null;
        //    }
        //    List<IDbCommand> l = new List<IDbCommand>();
        //    using (IDbConnection conn = Connection())
        //    {
        //        IDbTransaction txn = null;    // BEGIN / START TRANSACTION は後で実装
        //        foreach (SQLPart sql in sqls.Items)
        //        {
        //            IDbCommand cmd = GetSqlCommand(sql.SQL, conn, txn);
        //            l.Add(cmd);
        //        }
        //    }
        //    return l.ToArray();
        //}
    }
}
