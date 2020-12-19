using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class CommentChangedEventArgs : EventArgs
    {
        public Comment Comment { get; private set; }
        internal CommentChangedEventArgs(Comment comment)
        {
            Comment = comment;
        }
    }

    public interface ICommentable : IDb2SourceInfo
    {
        string GetSqlType();
        Comment Comment { get; set; }
        void OnCommentChanged(CommentChangedEventArgs e);
    }

    public class Comment : NamedObject, IDb2SourceInfo
    {
        public Db2SourceContext Context { get; private set; }
        public Schema Schema { get; private set; }
        public string SchemaName
        {
            get
            {
                return Schema?.Name;
            }
        }
        public virtual string GetCommentType()
        {
            return GetTarget()?.GetSqlType();
        }
        protected override string GetIdentifier()
        {
            return Context.GetEscapedIdentifier(SchemaName, Target, null);
        }
        /// <summary>
        /// コメントが編集されていたらtrueを返す
        /// 変更を重ねて元と同じになった場合はfalseを返す
        /// </summary>
        /// <returns></returns>
        public override bool IsModified() { return _text != _oldText; }
        private string _target;
        public string Target
        {
            get
            {
                return _target;
            }
            set
            {
                if (_target == value)
                {
                    return;
                }
                _target = value;
                InvalidateIdentifier();
            }
        }
        private string _text;
        private string _oldText;
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnTextChanged(EventArgs.Empty);
                }
            }
        }
        public virtual ICommentable GetTarget()
        {
            SchemaObject o = Schema.Objects[Target];
            return o;
        }
        public void Link()
        {
            ICommentable o = GetTarget();
            if (o != null)
            {
                o.Comment = this;
            }
        }

        protected void OnTextChanged(EventArgs e)
        {
            GetTarget()?.OnCommentChanged(new CommentChangedEventArgs(this));
        }
        public virtual string EscapedIdentifier(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, Target, baseSchemaName);
        }
        internal Comment(Db2SourceContext context, string schema) : base(context.RequireSchema(schema).Comments)
        {
            Context = context;
            Schema = context.RequireSchema(schema);
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal Comment(Db2SourceContext context, string schema, string target, string comment, bool isLoaded) : base(context.RequireSchema(schema).Comments)
        {
            Context = context;
            Schema = context.RequireSchema(schema);
            Target = target;
            Text = comment;
            if (isLoaded)
            {
                _oldText = Text;
            }
        }
        public override void Release()
        {
            if (Schema != null)
            {
                Schema.Comments.Remove(this);
            }
        }
    }

    public class ColumnComment : Comment
    {
        public override string GetCommentType()
        {
            return "COLUMN";
        }
        private string _column;
        public string Column
        {
            get
            {
                return _column;
            }
            set
            {
                _column = value;
                InvalidateIdentifier();
            }
        }
        protected override string GetIdentifier()
        {
            return base.GetIdentifier() + "." + _column;
        }
        public override ICommentable GetTarget()
        {
            Selectable o = Schema?.Objects[Target] as Selectable;
            if (o == null)
            {
                return null;
            }
            Column c = o.Columns[Column];
            return c;
        }
        public override string EscapedIdentifier(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, new string[] { Target, Column }, baseSchemaName);
        }
        internal ColumnComment(Db2SourceContext context, string schema) : base(context, schema) { }
        internal ColumnComment(Db2SourceContext context, string schema, string table, string column, string comment, bool isLoaded) : base(context, schema, table, comment, isLoaded)
        {
            Column = column;
        }
    }
}
