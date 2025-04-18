﻿using System;
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

        private string _schemaName;
        public string SchemaName
        {
            get
            {
                return _schemaName;
			}
            private set
            {
                if (_schemaName != value)
                {
                    return;
                }
                _schemaName = value;
                InvalidateIdentifier();
				OnPropertyChanged(nameof(SchemaName));
            }
        }
        public virtual string GetCommentType()
        {
            return GetTarget()?.GetSqlType();
        }
        protected override string GetFullIdentifier()
        {
            return Db2SourceContext.JointIdentifier(SchemaName, Target);
        }
        protected override string GetIdentifier()
        {
            return Target;
        }

        protected override int GetIdentifierDepth()
        {
            return 2;
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
            if (FullIdentifier != obj.FullIdentifier)
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
            SchemaObject o = Context.Objects[SchemaName, Target];
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

        internal Comment(Db2SourceContext context, string schema) : base(context.Comments)
        {
            Context = context;
            _schemaName = schema;
        }

        internal Comment(Db2SourceContext context, string schema, string target, string owner, string comment, bool isLoaded) : base(context.Comments)
        {
            Context = context;
			_schemaName = schema;
			Target = target;
            Owner = owner;
            Text = comment;
            if (isLoaded)
            {
                _oldText = Text;
            }
        }
        public override NamespaceIndex GetCollectionIndex()
        {
            return NamespaceIndex.Comments;
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }
            if (disposing)
            {
                Context.Comments.Remove(this);
            }
            base.Dispose(disposing);
        }
        public override void Release()
        {
            base.Release();
            Context.Comments.Invalidate();
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
        protected override string GetFullIdentifier()
        {
            return Db2SourceContext.JointIdentifier(SchemaName, Target, _column);
        }
        protected override string GetIdentifier()
        {
            return Db2SourceContext.JointIdentifier(Target, _column);
        }
        protected override int GetIdentifierDepth()
        {
            return 3;
        }
        public override ICommentable GetTarget()
        {
            return Context.Columns[SchemaName, Target, Column];
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

    public class StoredProcecureBaseComment : Comment
    {
        private string[] _arguments = StrUtil.EmptyStringArray;
        protected override string GetFullIdentifier()
        {
            return base.GetFullIdentifier() + StrUtil.DelimitedText(_arguments, ",", "(", ")");
        }
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
            SchemaObject o = Context.Objects[SchemaName, Identifier];
            return o;
        }

        internal StoredProcecureBaseComment(Db2SourceContext context, string schema) : base(context, schema) { }
        internal StoredProcecureBaseComment(Db2SourceContext context, string schema, string func, string[] arguments, string comment, bool isLoaded) : base(context, schema, func, null, comment, isLoaded)
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
        protected override string GetFullIdentifier()
        {
            return Db2SourceContext.JointIdentifier(SchemaName, Owner) + "@" + Target;
        }
        protected override string GetIdentifier()
        {
            return Owner + "@" + Target;
        }
        public override string EscapedTargetName(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(Target, true);
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
            Selectable o = Context.Selectables[SchemaName, Owner];
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
            Table o = Context.Tables[SchemaName, Owner];
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
