using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    partial class NpgsqlDataSet
    {
        public interface ISyntaxTreeNode
        {
            PgsqlToken[] Tokens { get; }
            ISyntaxTreeNode Next { get; set; }
            ISyntaxTreeNode FirstChild { get; set; }
            ISyntaxTreeNode Parent { get; set; }
            object Data { get; set; }
        }

        public class IdentifierNode<T>: ISyntaxTreeNode where T : SchemaObject
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
            object ISyntaxTreeNode.Data
            {
                get
                {
                    return _data;
                }
                set
                {
                    if (value != null && (!value.GetType().IsSubclassOf(typeof(T)) && (value.GetType() != typeof(T))))
                    {
                        throw new ArgumentException(string.Format("{0} Dataに{1}型の値を代入しようとしています", typeof(T).Name, value.GetType().Name));
                    }
                    _data = value as T;
                }
            }

            public ISyntaxTreeNode FirstChild { get; set; }
            public ISyntaxTreeNode Next { get; set; }
            public ISyntaxTreeNode Parent { get; set; }
            public PgsqlToken[] Tokens { get; private set; }
            public IdentifierNode(PgsqlToken token, ISyntaxTreeNode parent, ISyntaxTreeNode next, T data)
            {
                Tokens = new PgsqlToken[] { token };
                Parent = parent;
                Next = next;
                _data = data;
            }
        }
    }
}
