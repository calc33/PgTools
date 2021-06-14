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
            DoubleColon = 0x3a3a,   // ::
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
        // Virtual Token
            Expr    = 0x7f000001,   // EXPR
            Number  = 0x7f000002,   // NUMBER
            Field   = 0x7f000003,   // column or table
            Fields  = 0x7f000004,   // columns or tables
            As      = 0x7f000005,   // AS
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
        public struct TokenPosition: IComparable
        {
            /// <summary>
            /// 文字位置(0から始まる)
            /// </summary>
            public int Index { get; set; }
            /// <summary>
            /// 行番号(1から始まる)
            /// </summary>
            public int Line { get; set; }
            /// <summary>
            /// 列番号(1から始まる)
            /// </summary>
            public int Column { get; set; }
            public TokenPosition(int index, int line, int column)
            {
                Index = index;
                Line = line;
                Column = column;
            }
            public TokenPosition(TokenizedSQL sql, int index)
            {
                // 前回の位置情報を使って高速化(可能なら)
                TokenPosition previous = sql._lastPos;
                if (index < previous.Index)
                {
                    previous = Empty;
                }
                string text = sql.Sql;
                Line = previous.Line;
                Column = previous.Column;
                Index = previous.Index;
                int i = Index;
                int l = Line;
                int c = Column;
                int n = Math.Min(index, text.Length - 1);
                bool wasCR = false;
                bool wasLF = false;
                while (i <= n)
                {
                    char ch = text[i];
                    if (wasCR && ch != '\n' || wasLF)
                    {
                        l++;
                        c = 1;
                    }

                    Index = i;
                    Line = l;
                    Column = c;

                    i++;
                    c++;
                    if (char.IsSurrogate(ch))
                    {
                        i++;
                        c++;
                    }
                    wasCR = (ch == '\r');
                    wasLF = (ch == '\n');
                }
                sql._lastPos = this;
            }
            public static readonly TokenPosition Empty = new TokenPosition(0, 1, 1);

            public int CompareTo(object obj)
            {
                if (!(obj is TokenPosition))
                {
                    return -1;
                }
                return Index.CompareTo(((TokenPosition)obj).Index);
            }
            public override bool Equals(object obj)
            {
                if (!(obj is TokenPosition))
                {
                    return false;
                }
                return Index == ((TokenPosition)obj).Index;
            }
            public override int GetHashCode()
            {
                return Index.GetHashCode();
            }
        }

        public class Token
        {
            public TokenKind Kind { get; private set; }
            public TokenID ID { get; set; }
            private readonly TokenizedSQL _owner;
            public TokenPosition Start { get; private set; }
            public TokenPosition End { get; private set; }

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
                if (End.Index < Start.Index)
                {
                    _value = string.Empty;
                    return;
                }
                _value = _owner.Sql.Substring(Start.Index, End.Index - Start.Index + 1);
                return;
            }
            public int HitTest(int pos)
            {
                if (pos < Start.Index)
                {
                    return -1;
                }
                if (End.Index < pos)
                {
                    return 1;
                }
                return 0;
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
                Start = new TokenPosition(owner, start);
                End = new TokenPosition(owner, current - 1);
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
                Start = new TokenPosition(owner, start);
                End = new TokenPosition(owner, current - 1);
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
                Start = new TokenPosition(owner, start);
                End = new TokenPosition(owner, current - 1);
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
                Start = new TokenPosition(owner, start);
                End = new TokenPosition(owner, current - 1);
            }
            internal void Joint(Token token)
            {
                End = token.End;
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

        public partial class TokenizedSQL
        {
            private readonly string _sql;
            public string Sql { get { return _sql; } }
            public Token[] Tokens { get; private set; }

            public Token Selected { get; set; }
            internal TokenPosition _lastPos = TokenPosition.Empty;

            public string Extract(Token startToken, Token endToken)
            {
                return _sql.Substring(startToken.Start.Index, endToken.End.Index - startToken.Start.Index + 1);
            }

            private int FindTokenIndexRecursive(int pos, int start, int end)
            {
                if (end < start)
                {
                    return -1;
                }
                int i = (start + end) / 2;
                int hit = Tokens[i].HitTest(pos);
                if (hit == 0)
                {
                    return i;
                }
                if (hit < 0)
                {
                    return FindTokenIndexRecursive(pos, start, i - 1);
                }
                return FindTokenIndexRecursive(pos, i + 1, end);
            }
            private int _lastSearchIndex = -1;
            public Token GetTokenAt(int pos)
            {
                int i;
                int i0 = 0;
                int i1 = Tokens.Length - 1;
                //前回探索位置の次を探索するケースが多いという想定で直前直後をまず探索
                if (_lastSearchIndex != -1)
                {
                    i = _lastSearchIndex;
                    int hit = Tokens[i].HitTest(pos);
                    if (hit == 0)
                    {
                        return Tokens[i];
                    }
                    if (hit < 0)
                    {
                        i--;
                        if (i0 <= i)
                        {
                            hit = Tokens[i].HitTest(pos);
                            if (hit == 0)
                            {
                                return Tokens[i];
                            }
                        }
                        i1 = i - 1;
                    }
                    else
                    {
                        i++;
                        if (i <= i1)
                        {
                            hit = Tokens[i].HitTest(pos);
                            if (hit == 0)
                            {
                                return Tokens[i];
                            }
                        }
                    }
                }
                //二分探索
                i = FindTokenIndexRecursive(pos, i0, i1);
                _lastSearchIndex = i;
                if (i == -1)
                {
                    return null;
                }
                return Tokens[i];
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
            private static readonly Dictionary<char, bool> IdentifierEndChars = InitIdentifierEndChars();
            
            private void AddToken(List<Token> tokens, int selectedPos, Token token)
            {
                tokens.Add(token);
                if (token.ID == TokenID.Space)
                {
                    if (token.Start.Index < selectedPos && selectedPos < token.End.Index)
                    {
                        Selected = token;
                    }
                }
                else
                {
                    if (token.Start.Index <= selectedPos && selectedPos <= token.End.Index)
                    {
                        Selected = token;
                    }
                }
            }
            private void Lex(int selectedPos)
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
                            AddToken(tokens, selectedPos, new Token(this, isNewLine ? TokenKind.NewLine : TokenKind.Space, TokenID.Space, p0, p));
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
                                AddToken(tokens, selectedPos, new Token(this, TokenKind.Identifier, TokenID.Identifier, p0, p));
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
                                AddToken(tokens, selectedPos, new Token(this, TokenKind.Literal, TokenID.Literal, p0, p));
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
                                AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, c, p0, p));
                                break;
                            case ':':
                                c2 = _sql[p + 1];
                                if(c2 == ':')
                                {
                                    p += 2;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, TokenID.DoubleColon, p0, p));
                                }
                                else
                                {
                                    p++;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, c, p0, p));
                                }
                                break;
                            case ';':
                                p++;
                                AddToken(tokens, selectedPos, new Token(this, TokenKind.Semicolon, c, p0, p));
                                break;
                            case '<':
                                c2 = _sql[p + 1];
                                if (c2 == '=' || c2 == '<' || c2 == '>')
                                {
                                    p += 2;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, new char[] { c, c2 }, p0, p));
                                }
                                else
                                {
                                    p++;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, c, p0, p));
                                }
                                break;
                            case '>':
                                c2 = _sql[p + 1];
                                if (c2 == '=' || c2 == '>')
                                {
                                    p += 2;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, new char[] { c, c2 }, p0, p));
                                }
                                else
                                {
                                    p++;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, c, p0, p));
                                }
                                break;
                            case '~':
                                c2 = _sql[p + 1];
                                if (c2 == '*')
                                {
                                    p += 2;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, new char[] { c, c2 }, p0, p));
                                }
                                else
                                {
                                    p++;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, c, p0, p));
                                }
                                break;
                            case '/':
                                if (_sql[p + 1] != '*')
                                {
                                    p++;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, c, p0, p));
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
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Comment, TokenID.Comment, p0, p));
                                }
                                break;
                            case '-':
                                if (_sql[p + 1] != '-')
                                {
                                    p++;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Comment, c, p0, p));
                                }
                                else
                                {
                                    for (p += 2; p < n && _sql[p] != '\r' && _sql[p] != '\n'; p++) ;
                                    if (_sql[p] == '\r' && _sql[p + 1] == '\n')
                                    {
                                        p++;
                                    }
                                    p++;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, TokenID.Comment, p0, p));
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
                                AddToken(tokens, selectedPos, new Token(this, TokenKind.DefBody, TokenID.DefBody, p0, p));
                                break;
                            case '!':
                                c2 = _sql[p + 1];
                                switch (c2)
                                {
                                    case '=':
                                        p += 2;
                                        AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, "<>", p0, p));
                                        break;
                                    case '~':
                                        if (_sql[p + 2] == '*')
                                        {
                                            p += 3;
                                            AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, "!~*", p0, p));
                                        }
                                        else
                                        {
                                            p += 2;
                                            AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, "!~", p0, p));
                                        }
                                        break;
                                    case '!':
                                        p += 2;
                                        AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, "!!", p0, p));
                                        break;
                                    default:
                                        p++;
                                        AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, c, p0, p));
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
                                            AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, "||/", p0, p));
                                        }
                                        else
                                        {
                                            p += 2;
                                            AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, "||", p0, p));
                                        }
                                        break;
                                    case '/':
                                        p += 2;
                                        AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, "|/", p0, p));
                                        break;
                                    default:
                                        p++;
                                        AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, c, p0, p));
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
                                AddToken(tokens, selectedPos, new Token(this, TokenKind.Operator, c, p0, p));
                                break;
                            default:
                                if (char.IsDigit(_sql, p))
                                {
                                    for (p++; p < n && (char.IsNumber(_sql, p) || _sql[p] == '.'); p++) ;
                                    AddToken(tokens, selectedPos, new Token(this, TokenKind.Numeric, '0', p0, p));
                                    break;
                                }
                                for (p++; p < n && !char.IsWhiteSpace(_sql, p) && !IdentifierEndChars.ContainsKey(_sql[p]); p++)
                                {
                                    if (char.IsSurrogate(_sql, p))
                                    {
                                        p++;
                                    }
                                }
                                AddToken(tokens, selectedPos, new Token(this, TokenKind.Identifier, 'A', p0, p));
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
                Lex(-1);
            }
            public TokenizedSQL(string sql, int selectedPos)
            {
                _sql = sql;
                Lex(selectedPos);
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
            if (!noQuote && NeedQuotedPgsqlIdentifier(s))
            {
                s = GetQuotedIdentifier(s);
            }
            return s;
        }
        private string NormalizeDefBody(string sql, CaseRule reservedRule, CaseRule identifierRule)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return sql;
            }
            if (sql[0] != '$' || sql[sql.Length - 1] != '$')
            {
                return sql;
            }
            int p1 = sql.IndexOf('$', 1);
            string mark1 = sql.Substring(0, p1 + 1);
            int p2 = sql.LastIndexOf('$', sql.Length - 2);
            string mark2 = sql.Substring(p2);
            string body = sql.Substring(p1 + 1, p2 - p1 - 1);
            body = NormalizeSQL(body, reservedRule, identifierRule);
            return mark1 + body + mark2;
        }

        public override string NormalizeSQL(string sql, CaseRule reservedRule, CaseRule identifierRule)
        {
            StringBuilder buf = new StringBuilder();
            TokenizedSQL tsql = new TokenizedSQL(sql);
            bool wasLanguage = false;
            bool isSqlDef = false;
            foreach (Token t in tsql.Tokens)
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
            Token holdedSpc = null;
            foreach (Token t in tsql.Tokens)
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
            List<SQLPart> l = new List<SQLPart>();
            TokenizedSQL tsql = new TokenizedSQL(sql);
            int i = 0;
            int n = tsql.Tokens.Length;
            while (i < n)
            {
                //Token t = tsql.Tokens[i];
                for (; i < n; i++)
                {
                    Token t = tsql.Tokens[i];
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
                    Offset = t0.Start.Index,
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
