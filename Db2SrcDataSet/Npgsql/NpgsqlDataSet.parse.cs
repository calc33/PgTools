using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    partial class NpgsqlDataSet
    {
        public class SyntaxTreeNode
        {
            //public object Data
            //{
            //    get
            //    {
            //        return _data;
            //    }
            //    set
            //    {
            //        if (value != null && (!value.GetType().IsSubclassOf(typeof(T)) && (value.GetType() != typeof(T))))
            //        {
            //            throw new ArgumentException(string.Format("{0} Dataに{1}型の値を代入しようとしています", typeof(T).Name, value.GetType().Name));
            //        }
            //        _data = value as T;
            //    }
            //}

            public SyntaxTreeNode FirstChild { get; private set; }
            public SyntaxTreeNode Next { get; private set; }
            public SyntaxTreeNode Prior { get; private set; }
            public SyntaxTreeNode Parent { get; private set; }
            public Token FirstToken { get; private set; }
            private Token _lastToken;
            internal void InvalidateLastToken()
            {
                _lastToken = null;
                Parent?.InvalidateLastToken();
            }
            private void UpdateLastTokn()
            {
                if (_lastToken != null)
                {
                    return;
                }
                _lastToken = FirstToken;
                for (SyntaxTreeNode node = FirstChild; node != null; node = node.Next)
                {
                    _lastToken = node.LastToken;
                }
            }
            public Token LastToken
            {
                get
                {
                    UpdateLastTokn();
                    return _lastToken;
                }
            }
            public Token[] Tokens { get; private set; }
            public SyntaxTreeNode(Token token, SyntaxTreeNode prior, SyntaxTreeNode parent)
            {
                if (prior != null && parent != null)
                {
                    throw new ArgumentException("prior, parentのうち片方にのみ値をセットできます");
                }
                FirstToken = token;
                Tokens = null;
                Parent = parent;
                Prior = prior;
                if (Prior != null)
                {
                    Next = Prior.Next;
                    Prior.Next = this;
                    if (Parent == null)
                    {
                        Parent = Prior.Parent;
                    }
                }
                else if (Parent != null)
                {
                    Parent.FirstChild = this;
                }
            }
        }

        public class IdentifierNode<T>: SyntaxTreeNode where T : SchemaObject
        {
            private T _data;
            public T Data
            {
                get
                {
                    return _data;
                }
                set
                {
                    _data = value;
                }
            }
            //public Type[] TypesGuessed { get; set; } = new Type[0];

            public IdentifierNode(Token token, SyntaxTreeNode prior, SyntaxTreeNode parent, T data) : base(token, prior, parent)
            {
                _data = data;
            }
        }

        public class ReservedWordNode : SyntaxTreeNode
        {
            public object Data { get; set; }
            public ReservedWordNode(Token token, SyntaxTreeNode prior, SyntaxTreeNode parent) : base(token, prior, parent)
            {
                Data = token.Value.ToLower();
            }
        }
        public class ExprNode : SyntaxTreeNode
        {
            public ExprNode(Token token, SyntaxTreeNode prior, SyntaxTreeNode parent) : base(token, prior, parent) { }
            private static Dictionary<TokenID, TokenID[][]> ParseRules = new Dictionary<TokenID, TokenID[][]>()
            {
                {
                    TokenID.Number,
                    new TokenID[][]
                    {
                        new TokenID[] { TokenID.Period, TokenID.Numeric },
                        new TokenID[] { TokenID.Numeric },
                        new TokenID[] { TokenID.Numeric, TokenID.Period, TokenID.Numeric },
                    }
                },
                {
                    TokenID.Expr,
                    new TokenID[][]
                    {
                        new TokenID[] { TokenID.Number },
                        new TokenID[] { TokenID.Identifier },
                        new TokenID[] { TokenID.Colon, TokenID.Identifier },
                        new TokenID[] { TokenID.Literal },
                        new TokenID[] { TokenID.Expr, TokenID.Period, TokenID.Identifier },
                        new TokenID[] { TokenID.Expr, TokenID.Comma, TokenID.Expr },
                        new TokenID[] { TokenID.ParenthesesL, TokenID.Expr, TokenID.ParenthesesR },
                        // 数式を展開
                    }
                },
                {
                    TokenID.Fields,
                    new TokenID[][]
                    {
                        new TokenID[] { TokenID.Field },
                        new TokenID[] { TokenID.Fields, TokenID.Comma, TokenID.Field },
                    }
                },
                {
                    TokenID.Field,
                    new TokenID[][]
                    {
                        new TokenID[] { TokenID.Expr },
                        new TokenID[] { TokenID.Expr, TokenID.Identifier },
                        new TokenID[] { TokenID.Expr, TokenID.As, TokenID.Identifier },
                    }
                }
            };
            private static TokenID[][] ExprRules = new TokenID[][]
            {
                new TokenID[] {}
            };
            public static ExprNode Parse(TokenizedSQL sql, Token token, ref int tokenIndex, SyntaxTreeNode prior, SyntaxTreeNode parent)
            {
                throw new NotImplementedException();
            }
        }

        public class StatementNode: SyntaxTreeNode
        {
            public StatementNode(Token token, SyntaxTreeNode prior, SyntaxTreeNode parent) : base(token, prior, parent) { }
        }
        public class TransactionNode : SyntaxTreeNode
        {
            public TransactionNode(Token token, SyntaxTreeNode prior, SyntaxTreeNode parent) : base(token, prior, parent) { }
        }

        internal delegate Node TokenToNode(SyntaxTreeNode current);
        internal class ParseRule
        {
            internal TokenID RuleId;
            internal int Priority;
            internal bool RightToLeft;
            internal TokenID[] Tokens;
            internal TokenToNode Rule;
            internal ParseRule(TokenID ruleId, int priority, bool rightToLeft, TokenID[] ids, TokenToNode rule)
            {
                RuleId = ruleId;
                Priority = priority;
                RightToLeft = rightToLeft;
                Tokens = ids;
                Rule = rule;
            }

            public override string ToString()
            {
                StringBuilder buf = new StringBuilder();
                buf.Append(RuleId.ToString());
                buf.Append(':');
                foreach (TokenID id in Tokens)
                {
                    buf.Append(' ');
                    buf.Append(id.ToString());
                }
                return buf.ToString();
            }
        }

        partial class TokenizedSQL
        {
            public SyntaxTreeNode First;
            //private ExprNode ParseExpr(Token token, ref int tokenIndex, SyntaxTreeNode prior, SyntaxTreeNode parent)
            //{
            //    throw new NotImplementedException();
            //}
            private StatementNode ParseSelect(Token token, ref int tokenIndex, SyntaxTreeNode prior)
            {
                StatementNode stmt = new StatementNode(token, prior, null);
                ReservedWordNode node = new ReservedWordNode(token, null, stmt);
                
                return stmt;
            }
            private SyntaxTreeNode ParseInternal(SyntaxTreeNode prior, ref int tokenIndex, int endIndex)
            {
                int i = tokenIndex;
                while (i <= endIndex)
                {
                    Token t = Tokens[i];
                    switch (t.Kind)
                    {
                        case TokenKind.NewLine:
                        case TokenKind.Space:
                        case TokenKind.Comment:
                            continue;
                        case TokenKind.Identifier:
                            switch (t.Value.ToLower())
                            {
                                case "select":
                                    StatementNode stmt = ParseSelect(t, ref i, prior);
                                    return stmt;
                                case "insert":
                                case "update":
                                case "delete":
                                case "alter":
                                case "create":
                                case "drop":
                                    return null;
                            }
                            throw new NotImplementedException();
                    }
                }
                return null;
            }
            public void Parse()
            {
                int i = 0;
                int n = Tokens.Length - 1;
                SyntaxTreeNode prior = null;
                while (i <= n)
                {
                    prior = ParseInternal(prior, ref i, n);
                }
            }
        }
    }
}
