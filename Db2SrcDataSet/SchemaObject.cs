using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Db2Source
{
    public enum CollectionOperation
    {
        Add,
        Remove,
        Update,
        AddRange,
        Clear
    }

    public class CollectionOperationEventArgs<T> : EventArgs
    {
        public string Property { get; private set; }
        public CollectionOperation Operation { get; private set; }
        /// <summary>
        /// Insert, RemoveAt など追加した位置が判明しているときに
        /// 通常は-1
        /// </summary>
        public int Index { get; private set; }
        /// <summary>
        /// Operation = Remove, Update の時に設定する
        /// </summary>
        public T OldValue { get; private set; }
        /// <summary>
        /// Operation = Add, Update の時に設定する
        /// </summary>
        public T NewValue { get; set; }
        internal CollectionOperationEventArgs(string property, CollectionOperation operation, int index, T newValue, T oldValue)
        {
            Property = property;
            Operation = operation;
            Index = index;
            NewValue = newValue;
            OldValue = oldValue;
        }
    }

    public class PropertyChangedEventArgs : EventArgs
    {
        public string Property { get; private set; }
        public object OldValue { get; private set; }
        public object NewValue { get; set; }
        internal PropertyChangedEventArgs(string property, object newValue, object oldValue)
        {
            Property = property;
            NewValue = newValue;
            OldValue = oldValue;
        }
    }
    
    public abstract partial class SchemaObject : NamedObject, ICommentable, IComparable
    {
        private Schema _schema;
        private string _name;
        public Db2SourceContext Context { get; private set; }
        public abstract string GetSqlType();
        public abstract string GetExportFolderName();

        public string Prefix { get; set; }
        public string Owner { get; set; }
        public Schema Schema { get { return _schema; } }
        public string SchemaName { get { return _schema?.Name; } }
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value)
                {
                    return;
                }
                string old = _name;
                _name = value;
                NameChanged(old);
            }
        }
        protected virtual void NameChanged(string oldValue)
        {
            InvalidateIdentifier();
        }

        public virtual string DisplayName
        {
            get
            {
                return _name;
            }
        }
        protected override string GetIdentifier()
        {
            return _name;
        }

        public string FullName
        {
            get
            {
                string s = SchemaName;
                if (!string.IsNullOrEmpty(s))
                {
                    s = s + ".";
                }
                return s + Name;
            }
        }
        public string EscapedIdentifier(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, Name, baseSchemaName);
        }

        public override bool IsModified()
        {
            if (base.IsModified())
            {
                return true;
            }
            if (_comment != null)
            {
                return _comment.IsModified();
            }
            return false;
        }

        private Comment _comment;
        public Comment Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                if (_comment == value)
                {
                    return;
                }
                CommentChangedEventArgs e = new CommentChangedEventArgs(value);
                _comment = value;
                OnCommentChanged(e);
            }
        }

        public string CommentText
        {
            get
            {
                return Comment?.Text;
            }
            set
            {
                if (Comment == null)
                {
                    Comment = new Comment(Context, SchemaName, Name, value, false);
                    Comment.Link();
                }
                Comment.Text = value;
            }
        }

        public string SqlDef { get; set; } = null;
        public TriggerCollection Triggers { get; private set; }

        public event EventHandler<PropertyChangedEventArgs> PropertyChanged;
        protected internal void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
        public event EventHandler<CommentChangedEventArgs> CommentChanged;
        void ICommentable.OnCommentChanged(CommentChangedEventArgs e)
        {
            CommentChanged?.Invoke(this, e);
        }

        protected internal void OnCommentChanged(CommentChangedEventArgs e)
        {
            CommentChanged?.Invoke(this, e);
        }

        public event EventHandler<CollectionOperationEventArgs<string>> UpdateColumnChanged;
        protected internal void OnUpdateColumnChanged(CollectionOperationEventArgs<string> e)
        {
            UpdateColumnChanged?.Invoke(this, e);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        internal SchemaObject(Db2SourceContext context, string owner, string schema, string objectName, Schema.CollectionIndex index) : base(context.RequireSchema(schema).GetCollection(index))
        {
            Context = context;
            Owner = owner;
            _schema = context.RequireSchema(schema);
            Name = objectName;
            Triggers = new TriggerCollection(this);
        }

        public override void Release()
        {
            Schema.GetCollection(GetCollectionIndex()).Remove(this);
            foreach (Trigger t in Triggers)
            {
                t.Release();
            }
        }

        public virtual void InvalidateColumns() { }
        public virtual void InvalidateConstraints() { }
        public virtual void InvalidateTriggers()
        {
            Triggers.Invalidate();
        }
        public virtual Schema.CollectionIndex GetCollectionIndex()
        {
            return Schema.CollectionIndex.Objects;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is SchemaObject))
            {
                return false;
            }
            SchemaObject sc = (SchemaObject)obj;
            return Schema.Equals(sc.Schema) && (Identifier == sc.Identifier);
        }
        public override int GetHashCode()
        {
            return ((Schema != null) ? Schema.GetHashCode() : 0) + (string.IsNullOrEmpty(Name) ? 0 : Name.GetHashCode());
        }
        public override string ToString()
        {
            return string.Format("{0} {1}", GetSqlType(), DisplayName);
        }

        public override int CompareTo(object obj)
        {
            if (!(obj is SchemaObject))
            {
                return -1;
            }
            SchemaObject sc = (SchemaObject)obj;
            int ret = Schema.CompareTo(sc.Schema);
            if (ret != 0)
            {
                return ret;
            }
            ret = string.Compare(Identifier, sc.Identifier);
            return ret;
        }
    }

    public interface IDb2SourceInfo
    {
        Db2SourceContext Context { get; }
        Schema Schema { get; }
    }

    public interface IDbTypeDef
    {
        string BaseType { get; set; }
        int? DataLength { get; set; }
        int? Precision { get; set; }
        bool? WithTimeZone { get; set; }
        bool IsSupportedType { get; set; }
        Type ValueType { get; set; }
    }

    public static class DbTypeDefUtil
    {
        public static string ToTypeText(IDbTypeDef def)
        {
            if (!def.DataLength.HasValue && !def.Precision.HasValue)
            {
                return def.BaseType;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append(def.BaseType);
            buf.Append('(');

            buf.Append(def.DataLength.HasValue ? def.DataLength.Value.ToString() : "*");
            if (def.Precision.HasValue)
            {
                buf.Append(',');
                buf.Append(def.Precision.Value);
            }
            buf.Append(')');
            if (def.WithTimeZone.HasValue && def.WithTimeZone.Value)
            {
                buf.Append(" with time zone");
            }
            return buf.ToString();
        }
    }
}
