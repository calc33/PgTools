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
            return Context.GetEscapedIdentifier(SchemaName, Target, null, true);
        }

        /// <summary>
        /// コメントが編集されていたらtrueを返す
        /// 変更を重ねて元と同じになった場合はfalseを返す
        /// </summary>
        /// <returns></returns>
        public override bool IsModified
        {
            get
            {
                return _text != _oldText;
            }
        }
        public override bool HasBackup()
        {
            return true;
        }
        public override void Backup(bool force)
        {
            _oldText = _text;
        }

        public override void Restore()
        {
            _text = _oldText;
        }

        public override bool ContentEquals(NamedObject obj)
        {
            if (GetType() != obj.GetType())
            {
                return false;
            }
            if (Identifier != obj.Identifier)
            {
                return false;
            }
            Comment c = (Comment)obj;
            return Text == c.Text;
        }
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

        private string _owner;
        public string Owner
        {
            get
            {
                return _owner;
            }
            set
            {
                if (_owner == value)
                {
                    return;
                }
                _owner = value;
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
            SchemaObject o = Schema?.Objects[Target];
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
        public virtual string EscapedTargetName(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, Target, baseSchemaName, true);
        }

        public virtual string EscapedOwnerName(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, Owner, baseSchemaName, true);
        }

        internal Comment(Db2SourceContext context, string schema) : base(context.RequireSchema(schema).Comments)
        {
            Context = context;
            Schema = context.RequireSchema(schema);
        }

        internal Comment(Db2SourceContext context, string schema, string target, string owner, string comment, bool isLoaded) : base(context.RequireSchema(schema)?.Comments)
        {
            Context = context;
            Schema = context.RequireSchema(schema);
            Target = target;
            Owner = owner;
            Text = comment;
            if (isLoaded)
            {
                _oldText = Text;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }
            if (disposing)
            {
                if (Schema != null)
                {
                    Schema.Comments.Remove(this);
                }
            }
            base.Dispose(disposing);
        }
        public override void Release()
        {
            base.Release();
            if (Schema != null)
            {
                Schema.Comments.Invalidate();
            }
            Schema = null;
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
        public override string EscapedTargetName(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, new string[] { Target, Column }, baseSchemaName, true);
        }
        internal ColumnComment(Db2SourceContext context, string schema) : base(context, schema) { }
        internal ColumnComment(Db2SourceContext context, string schema, string table, string column, string comment, bool isLoaded) : base(context, schema, table, null, comment, isLoaded)
        {
            Column = column;
        }
    }

    public class StoredFunctionComment : Comment
    {
        private string[] _arguments = StrUtil.EmptyStringArray;
        protected override string GetIdentifier()
        {
            return base.GetIdentifier() + StrUtil.DelimitedText(_arguments, ",", "(", ")");
        }

        public override string EscapedTargetName(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, Target, baseSchemaName, true)
                + StrUtil.DelimitedText(_arguments, ",", "(", ")");
        }

        public override ICommentable GetTarget()
        {
            SchemaObject o = Schema?.Objects[Identifier];
            return o;
        }

        internal StoredFunctionComment(Db2SourceContext context, string schema) : base(context, schema) { }
        internal StoredFunctionComment(Db2SourceContext context, string schema, string func, string[] arguments, string comment, bool isLoaded) : base(context, schema, func, null, comment, isLoaded)
        {
            _arguments = arguments;
        }
    }

    public abstract class SubObjectComment : Comment
    {
        internal SubObjectComment(Db2SourceContext context, string schema) : base(context, schema) { }
        internal SubObjectComment(Db2SourceContext context, string schema, string table, string subObject, string comment, bool isLoaded) : base(context, schema, subObject, table, comment, isLoaded)
        {
        }
        protected override string GetIdentifier()
        {
            return Target + "@" + Context.GetEscapedIdentifier(SchemaName, Owner, null, true);
        }
        public override string EscapedTargetName(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(null, Target, null, true);
        }
    }
    public class TriggerComment : SubObjectComment
    {
        public override string GetCommentType()
        {
            return "TRIGGER";
        }
        public override ICommentable GetTarget()
        {
            Selectable o = Schema?.Objects[Owner] as Selectable;
            if (o == null)
            {
                return null;
            }
            Trigger trigger = o.Triggers[Target];
            return trigger;
        }
        internal TriggerComment(Db2SourceContext context, string schema) : base(context, schema) { }
        internal TriggerComment(Db2SourceContext context, string schema, string table, string trigger, string comment, bool isLoaded)
            : base(context, schema, table, trigger, comment, isLoaded) { }
    }

    public class ConstraintComment : SubObjectComment
    {
        public override string GetCommentType()
        {
            return "CONSTRAINT";
        }
        public override ICommentable GetTarget()
        {
            Table o = Schema?.Objects[Owner] as Table;
            if (o == null)
            {
                return null;
            }
            Constraint constraint = o.Constraints[Target];
            return constraint;
        }
        internal ConstraintComment(Db2SourceContext context, string schema) : base(context, schema) { }
        internal ConstraintComment(Db2SourceContext context, string schema, string table, string constraint, string comment, bool isLoaded)
            : base(context, schema, table, constraint, comment, isLoaded) { }
    }
}
