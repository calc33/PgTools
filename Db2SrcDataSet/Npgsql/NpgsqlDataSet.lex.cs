using System;
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
        }
        public enum TokenKind
        {
            Space,
            NewLine,
            Identifier,
            Numeric,
            Literal,
            Comment,
            Operator,
            Semicolon,
            DefBody
        }
        public class Token
        {
            public TokenKind Kind { get; private set; }
            public TokenID ID { get; set; }
            private TokenizedSQL _owner;
            public int StartPos { get; private set; }
            public int EndPos { get; private set; }
            private string _value = null;
            private void UpdateValue()
            {
                if (_value != null)
                {
                    return;
                }
                if (_owner == null)
                {
                    _value = string.Empty;
                    return;
                }
                if (EndPos < StartPos)
                {
                    _value = string.Empty;
                    return;
                }
                _value = _owner.Sql.Substring(StartPos, EndPos - StartPos + 1);
                return;
            }
            private void InvalidateValue()
            {
                _value = null;
            }
            public string Value
            {
                get
                {
                    UpdateValue();
                    return _value;
                }
            }
            internal Token(TokenizedSQL owner, TokenKind kind, TokenID identifier, int start, int current)
            {
                _owner = owner;
                Kind = kind;
                ID = identifier;
                StartPos = start;
                EndPos = current - 1;
            }
            internal Token(TokenizedSQL owner, TokenKind kind, char identifier, int start, int current)
            {
                _owner = owner;
                Kind = kind;
                byte[] bytes = Encoding.ASCII.GetBytes(new char[] { identifier }, 0, 1);
                int v = 0;
                foreach (byte b in bytes)
                {
                    v = v << 8 | b;
                }
                ID = (TokenID)v;
                StartPos = start;
                EndPos = current - 1;
            }
            internal Token(TokenizedSQL owner, TokenKind kind, char[] ids, int start, int current)
            {
                _owner = owner;
                Kind = kind;
                byte[] bytes = Encoding.ASCII.GetBytes(ids);
                int v = 0;
                foreach (byte b in bytes)
                {
                    v = v << 8 | b;
                }
                ID = (TokenID)v;
                StartPos = start;
                EndPos = current - 1;
            }
            internal Token(TokenizedSQL owner, TokenKind kind, string ids, int start, int current)
            {
                _owner = owner;
                Kind = kind;
                byte[] bytes = Encoding.ASCII.GetBytes(ids);
                int v = 0;
                foreach (byte b in bytes)
                {
                    v = v << 8 | b;
                }
                ID = (TokenID)v;
                StartPos = start;
                EndPos = current - 1;
            }
            internal void Joint(Token token)
            {
                EndPos = token.EndPos;
                InvalidateValue();
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

        public class TokenizedSQL
        {
            private string _sql;
            public string Sql { get { return _sql; } }
            public Token[] Tokens { get; private set; }

            public string Extract(Token startToken, Token endToken)
            {
                return _sql.Substring(startToken.StartPos, endToken.EndPos - startToken.StartPos + 1);
            }

            private static Dictionary<char, bool> InitIdentifierEndChars()
            {
                Dictionary<char, bool> dict = new Dictionary<char, bool>();
                foreach (char c in " !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~")
                {
                    dict.Add(c, true);
                }
                return dict;
            }
            private static readonly Dictionary<char, bool> IdentifierEndChars = InitIdentifierEndChars();
            //private void Lex
            private void Lex()
            {
                List<Token> tokens = new List<Token>();
                try
                {
                    int p = 0;
                    int n = _sql.Length;
                    while (p < n)
                    {
                        int p0 = p;
                        bool isNewLine = false;
                        while (p < n && char.IsWhiteSpace(_sql, p))
                        {
                            char sp = _sql[p];
                            if (sp == '\u0010' || sp == '\u0013')
                            {
                                isNewLine = true;
                            }
                            p++;
                        }
                        if (p0 < p)
                        {
                            tokens.Add(new Token(this, isNewLine ? TokenKind.NewLine : TokenKind.Space, TokenID.Space, p0, p));
                        }
                        if (!(p < n))
                        {
                            return;
                        }
                        p0 = p;
                        char c = _sql[p];
                        char c2;
                        switch (c)
                        {
                            case '"':
                                for (p++; p < n && _sql[p] != '"'; p++)
                                {
                                    if (char.IsSurrogate(_sql[p]))
                                    {
                                        p++;
                                    }
                                }
                                if (p < n)
                                {
                                    p++;
                                }
                                tokens.Add(new Token(this, TokenKind.Identifier, TokenID.Identifier, p0, p));
                                break;
                            case '\'':
                                for (p++; p < n && _sql[p] != '\''; p++)
                                {
                                    if (char.IsSurrogate(_sql[p]))
                                    {
                                        p++;
                                    }
                                }
                                if (p < n)
                                {
                                    p++;
                                }
                                tokens.Add(new Token(this, TokenKind.Literal, TokenID.Literal, p0, p));
                                break;
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
                                tokens.Add(new Token(this, TokenKind.Operator, c, p0, p));
                                break;
                            case ':':
                                c2 = _sql[p + 1];
                                if(c2 == ':')
                                {
                                    p += 2;
                                    tokens.Add(new Token(this, TokenKind.Operator, new char[] { c, c2 }, p0, p));
                                }
                                else
                                {
                                    p++;
                                    tokens.Add(new Token(this, TokenKind.Operator, c, p0, p));
                                }
                                break;
                            case ';':
                                p++;
                                tokens.Add(new Token(this, TokenKind.Semicolon, c, p0, p));
                                break;
                            case '<':
                                c2 = _sql[p + 1];
                                if (c2 == '=' || c2 == '<' || c2 == '>')
                                {
                                    p += 2;
                                    tokens.Add(new Token(this, TokenKind.Operator, new char[] { c, c2 }, p0, p));
                                }
                                else
                                {
                                    p++;
                                    tokens.Add(new Token(this, TokenKind.Operator, c, p0, p));
                                }
                                break;
                            case '>':
                                c2 = _sql[p + 1];
                                if (c2 == '=' || c2 == '>')
                                {
                                    p += 2;
                                    tokens.Add(new Token(this, TokenKind.Operator, new char[] { c, c2 }, p0, p));
                                }
                                else
                                {
                                    p++;
                                    tokens.Add(new Token(this, TokenKind.Operator, c, p0, p));
                                }
                                break;
                            case '~':
                                c2 = _sql[p + 1];
                                if (c2 == '*')
                                {
                                    p += 2;
                                    tokens.Add(new Token(this, TokenKind.Operator, new char[] { c, c2 }, p0, p));
                                }
                                else
                                {
                                    p++;
                                    tokens.Add(new Token(this, TokenKind.Operator, c, p0, p));
                                }
                                break;
                            case '/':
                                if (_sql[p + 1] != '*')
                                {
                                    p++;
                                    tokens.Add(new Token(this, TokenKind.Operator, c, p0, p));
                                }
                                else
                                {
                                    p++;
                                    while (p < n)
                                    {
                                        for (p++; p < n && _sql[p] != '*'; p++) ;
                                        if (_sql[p + 1] == '/')
                                        {
                                            p += 2;
                                            break;
                                        }
                                    }
                                    tokens.Add(new Token(this, TokenKind.Comment, TokenID.Comment, p0, p));
                                }
                                break;
                            case '-':
                                if (_sql[p + 1] != '-')
                                {
                                    p++;
                                    tokens.Add(new Token(this, TokenKind.Comment, c, p0, p));
                                }
                                else
                                {
                                    for (p += 2; p < n && _sql[p] != '\r' && _sql[p] != '\n'; p++) ;
                                    if (_sql[p] == '\r' && _sql[p + 1] == '\n')
                                    {
                                        p++;
                                    }
                                    p++;
                                    tokens.Add(new Token(this, TokenKind.Operator, TokenID.Comment, p0, p));
                                }
                                break;
                            case '$':
                                p0 = p;
                                for (p++; p < n && _sql[p] != '$'; p++) ;
                                string mark = _sql.Substring(p0, p - p0 + 1);
                                while (p < n)
                                {
                                    for (p++; p < n && _sql[p] != '$'; p++) ;
                                    if (p + mark.Length <= n && string.Compare(_sql.Substring(p, mark.Length), mark, true) == 0)
                                    {
                                        p += mark.Length;
                                        break;
                                    }
                                }
                                tokens.Add(new Token(this, TokenKind.DefBody, TokenID.DefBody, p0, p));
                                break;
                            case '!':
                                c2 = _sql[p + 1];
                                switch (c2)
                                {
                                    case '=':
                                        p += 2;
                                        tokens.Add(new Token(this, TokenKind.Operator, "<>", p0, p));
                                        break;
                                    case '~':
                                        if (_sql[p + 2] == '*')
                                        {
                                            p += 3;
                                            tokens.Add(new Token(this, TokenKind.Operator, "!~*", p0, p));
                                        }
                                        else
                                        {
                                            p += 2;
                                            tokens.Add(new Token(this, TokenKind.Operator, "!~", p0, p));
                                        }
                                        break;
                                    case '!':
                                        p += 2;
                                        tokens.Add(new Token(this, TokenKind.Operator, "!!", p0, p));
                                        break;
                                    default:
                                        p++;
                                        tokens.Add(new Token(this, TokenKind.Operator, c, p0, p));
                                        break;
                                }
                                break;
                            case '|':
                                c2 = _sql[p + 1];
                                switch (c2)
                                {
                                    case '|':
                                        if (_sql[p + 2] == '/')
                                        {
                                            p += 3;
                                            tokens.Add(new Token(this, TokenKind.Operator, "||/", p0, p));
                                        }
                                        else
                                        {
                                            p += 2;
                                            tokens.Add(new Token(this, TokenKind.Operator, "||", p0, p));
                                        }
                                        break;
                                    case '/':
                                        p += 2;
                                        tokens.Add(new Token(this, TokenKind.Operator, "|/", p0, p));
                                        break;
                                    default:
                                        p++;
                                        tokens.Add(new Token(this, TokenKind.Operator, c, p0, p));
                                        break;
                                }
                                break;
                            case '?':
                            case '@':
                            case '\\':
                            case '^':
                            //case '_':
                            case '`':
                            case '{':
                            case '}':
                                p++;
                                tokens.Add(new Token(this, TokenKind.Operator, c, p0, p));
                                break;
                            default:
                                if (char.IsDigit(_sql, p))
                                {
                                    for (p++; p < n && (char.IsNumber(_sql, p) || _sql[p] == '.'); p++) ;
                                    tokens.Add(new Token(this, TokenKind.Numeric, '0', p0, p));
                                    break;
                                }
                                for (p++; p < n && !char.IsWhiteSpace(_sql, p) && !IdentifierEndChars.ContainsKey(_sql[p]); p++)
                                {
                                    if (char.IsSurrogate(_sql, p))
                                    {
                                        p++;
                                    }
                                }
                                tokens.Add(new Token(this, TokenKind.Identifier, 'A', p0, p));
                                break;
                        }
                    }
                }
                finally
                {
                    Tokens = tokens.ToArray();
                }
            }

            public TokenizedSQL(string sql)
            {
                _sql = sql;
                Lex();
            }
        }

        public override SQLParts SplitSQL(string sql)
        {
            List<SQLPart> l = new List<SQLPart>();
            TokenizedSQL tsql = new TokenizedSQL(sql);
            int i = 0;
            int n = tsql.Tokens.Length;
            while (i < n)
            {
                Token t = tsql.Tokens[i];
                for (; i < n; i++)
                {
                    t = tsql.Tokens[i];
                    if (t.Kind != TokenKind.Space && t.Kind != TokenKind.NewLine && t.Kind != TokenKind.Comment)
                    {
                        break;
                    }
                }
                int i0 = i;
                Token t0 = tsql.Tokens[i0];
                bool endByNewLine = false;
                if (t0.Kind == TokenKind.Identifier && (string.Compare(t0.Value, "begin") == 0 || string.Compare(t0.Value, "start") == 0))
                {
                    endByNewLine = true;
                }
                for (; i < n && tsql.Tokens[i].Kind != TokenKind.Semicolon && (!endByNewLine || tsql.Tokens[i].Kind != TokenKind.NewLine); i++) ;
                string s = tsql.Extract(t0, tsql.Tokens[i - 1]);
                SQLPart sp = new SQLPart()
                {
                    Offset = t0.StartPos,
                    SQL = s,
                    ParameterNames = GetParameterNames(s).ToArray(),
                };
                l.Add(sp);
                i++;
            }

            return new SQLParts()
            {
                Items = l.ToArray(),
                ParameterNames = GetParameterNames(sql).ToArray()
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
