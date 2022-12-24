using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class TokenizedSQL
    {
        private readonly string _sql;
        public string Sql { get { return _sql; } }
        public Token[] Tokens { get; protected set; }
        public Token Selected { get; set; }

        public virtual string Extract(Token startToken, Token endToken)
        {
            return _sql.Substring(startToken.StartPos, endToken.EndPos - startToken.StartPos + 1);
        }

        private int FindTokenIndex(int characterPosition, int startIndex, int endIndex)
        {
            if (endIndex < startIndex)
            {
                return -1;
            }
            int i = startIndex;
            Token token = Tokens[i];
            if (characterPosition  < token.StartPos)
            {
                return startIndex - 1;
            }
            if (token.StartPos <= characterPosition && characterPosition <= token.EndPos)
            {
                return i;
            }
            i = endIndex;
            token = Tokens[i];
            if (token.EndPos < characterPosition)
            {
                return endIndex + 1;
            }
            if (token.StartPos <= characterPosition && characterPosition <= token.EndPos)
            {
                return i;
            }
            i = (startIndex + endIndex) / 2;
            token = Tokens[i];
            if (token.StartPos <= characterPosition && characterPosition <= token.EndPos)
            {
                return i;
            }
            if (characterPosition < token.StartPos)
            {
                return FindTokenIndex(characterPosition, startIndex + 1, i - 1);
            }
            else
            {
                return FindTokenIndex(characterPosition, i + 1, endIndex - 1);
            }
        }
        public int GetTokenIndexAt(int characterPosition)
        {
            return FindTokenIndex(characterPosition, 0, Tokens.Length - 1);
        }

        public TokenizedSQL(string sql)
        {
            _sql = sql;
        }
        //public TokenizedSQL(string sql, int selectedPos)
        //{
        //    _sql = sql;
        //}
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
        private readonly TokenizedSQL _owner;
        protected internal TokenizedSQL Owner { get { return _owner; } }
        public TokenKind Kind { get; private set; }
        public int ID { get; set; }
        public bool IsReservedWord { get; set; }
        public int StartPos { get; private set; }
        public int EndPos { get; private set; }
        private string _value = null;
        protected internal void UpdateValue()
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
        protected internal void InvalidateValue()
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
        protected internal Token(TokenizedSQL owner, int start, int current)
        {
            _owner = owner;
            StartPos = start;
            EndPos = current - 1;
        }
        protected internal Token(TokenizedSQL owner, TokenKind kind, int start, int current)
        {
            _owner = owner;
            Kind = kind;
            StartPos = start;
            EndPos = current - 1;
        }
        protected internal Token(TokenizedSQL owner, TokenKind kind, int identifier, int start, int current)
        {
            _owner = owner;
            Kind = kind;
            ID = identifier;
            StartPos = start;
            EndPos = current - 1;
        }
        protected internal void Joint(Token token)
        {
            EndPos = token.EndPos;
            InvalidateValue();
        }

        public override string ToString()
        {
            return Value;
        }
    }
    partial class Db2SourceContext
    {
        /// <summary>
        /// SQLを字句解析した結果を返す
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public abstract TokenizedSQL Tokenize(string sql);
        public abstract string[] ExtractDefBody(string sql, CaseRule reservedRule, CaseRule identifierRule);
    }
}
