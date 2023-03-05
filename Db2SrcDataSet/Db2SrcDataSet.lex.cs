using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public enum GapAlignment
    {
        Before,
        After
    }

    public class TokenizedSQL: IEnumerable<Token>
    {
        private readonly string _sql;
        public string Sql { get { return _sql; } }

        public virtual string Extract(Token startToken, Token endToken)
        {
            return _sql.Substring(startToken.StartPos, endToken.EndPos - startToken.StartPos + 1);
        }

        public Token QueryTokenByPosition(int position, GapAlignment alignment)
        {
            foreach (Token t in this)
            {
                if (t.IsHit(position, alignment) <= 0)
                {
                    return t;
                }
            }
            return null;
        }

        public Token[] QueryTokenByPosition(int[] positions, GapAlignment alignment)
        {
            if (positions == null)
            {
                throw new ArgumentNullException("positions");
            }
            int n = positions.Length;
            int nFound = 0;
            Token[] ret = new Token[n];
            foreach (Token t in this)
            {
                for (int i = 0; i < n; i++)
                {
                    if (ret[i] == null && t.IsHit(positions[i], alignment) <= 0)
                    {
                        ret[i] = t;
                        nFound++;
                        if (n <= nFound)
                        {
                            break;
                        }
                    }
                }
            }
            return ret;
        }
        public Token[] QueryTokenByPosition(int[] positions, GapAlignment[] alignments)
        {
            if (positions == null)
            {
                throw new ArgumentNullException("positions");
            }
            if (alignments == null)
            {
                throw new ArgumentNullException("alignments");
            }
            int n = positions.Length;
            if (alignments.Length != n)
            {
                throw new ArgumentException("positionとalignmentの要素数が異なります");
            }
            int nFound = 0;
            Token[] ret = new Token[n];
            foreach (Token t in this)
            {
                for (int i = 0; i < n; i++)
                {
                    if (ret[i] == null && t.IsHit(positions[i], alignments[i]) <= 0)
                    {
                        ret[i] = t;
                    }
                    nFound++;
                    if (n <= nFound)
                    {
                        break;
                    }
                }
            }
            return ret;
        }

        protected virtual IEnumerator<Token> GetEnumeratorCore()
        {
            throw new NotImplementedException();
        }
        public IEnumerator<Token> GetEnumerator()
        {
            return GetEnumeratorCore();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorCore();
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
        public TokenizedSQL Child { get; internal set; }
        public Token Parent { get; private set; }
        private string _value = null;

        public string GetValue()
        {
            if (_owner == null)
            {
                return string.Empty;
            }
            if (EndPos < StartPos)
            {
                return string.Empty;
            }
            return _owner.Sql.Substring(StartPos, EndPos - StartPos + 1);
        }

        protected internal void UpdateValue()
        {
            if (_value != null)
            {
                return;
            }
            _value = GetValue();
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

        /// <summary>
        /// positionで指定した文字にこのトークンがある場合は0を返す
        /// トークンの境界にある場合はalignmentでどちらに割り当てるか決める
        /// </summary>
        /// <param name="position">SQL文中の文字位置</param>
        /// <param name="alignment">positionがトークンの境界の場合、どちらに寄せるかを決定する</param>
        /// <returns></returns>
        public int IsHit(int position, GapAlignment alignment)
        {
            switch (alignment)
            {
                case GapAlignment.After:
                    return (StartPos <= position && position < EndPos + 1) ? 0 : position.CompareTo(StartPos);
                case GapAlignment.Before:
                    return (StartPos < position && position <= EndPos + 1) ? 0 : position.CompareTo(EndPos);
                default:
                    throw new NotImplementedException();
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
