using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
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

    public abstract partial class SchemaObject : NamedObject, ICommentable, IComparable, INotifyPropertyChanged
    {
        private Schema _schema;
        private string _name;
        //protected SchemaObject _old;
        public Db2SourceContext Context { get; private set; }
        public abstract string GetSqlType();
        public abstract string GetExportFolderName();

        public string Prefix { get; set; }
        public string Owner { get; set; }
        public Schema Schema
        {
            get
            {
                return _schema;
            }
            protected set
            {
                _schema = value;
            }
        }
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
                    s += ".";
                }
                return s + Name;
            }
        }
        public string EscapedIdentifier(string baseSchemaName)
        {
            return Context.GetEscapedIdentifier(SchemaName, Name, baseSchemaName, true);
        }

        //protected override void Backup() { }

        protected void RestoreFrom(SchemaObject backup)
        {
            Owner = backup.Owner;
            _schema = backup.Schema;
            Name = backup.Name;
            Triggers = new TriggerCollection(this, backup.Triggers);
        }

        //public override void Restore()
        //{
        //    if (_backup == null)
        //    {
        //        return;
        //    }
        //    RestoreFrom(_backup);
        //}

        public override bool ContentEquals(NamedObject obj)
        {
            SchemaObject o = (SchemaObject)obj;
            if (!base.ContentEquals(o))
            if (Triggers.Count != o.Triggers.Count)
            {
                return false;
            }
            foreach (Trigger t in Triggers)
            {
                int i = o.Triggers.IndexOf(t);
                if (i == -1)
                {
                    return false;
                }
                if (!t.ContentEquals(o.Triggers[i]))
                {
                    return false;
                }
            }
            return true;
        }

        //public override bool IsModified()
        //{
        //    return (_backup != null) && !ContentEquals(_backup);
        //}

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

        public event PropertyChangedEventHandler PropertyChanged;
        protected internal void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        void ICommentable.OnCommentChanged(CommentChangedEventArgs e)
        {
            OnPropertyChanged("CommentText");
        }

        protected internal void OnCommentChanged(CommentChangedEventArgs e)
        {
            OnPropertyChanged("CommentText");
        }

        public event EventHandler<CollectionOperationEventArgs<string>> UpdateColumnChanged;

        protected internal void OnUpdateColumnChanged(CollectionOperationEventArgs<string> e)
        {
            UpdateColumnChanged?.Invoke(this, e);
        }

        internal SchemaObject(Db2SourceContext context, string owner, string schema, string objectName, Schema.CollectionIndex index) : base(context.RequireSchema(schema)?.GetCollection(index))
        {
            Context = context;
            Owner = owner;
            _schema = context.RequireSchema(schema);
            Name = objectName;
            Triggers = new TriggerCollection(this);
        }
        protected SchemaObject(SchemaObject basedOn): base(null)
        {
            Context = basedOn.Context;
            Owner = basedOn.Owner;
            _schema = basedOn.Schema;
            Name = basedOn.Name;
            Triggers = new TriggerCollection(this);
            foreach (Trigger t in basedOn.Triggers)
            {
                Triggers.Add(new Trigger(t));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (Schema != null)
                    {
                        Schema.GetCollection(GetCollectionIndex())?.Remove(this);
                    }
                    foreach (Trigger t in Triggers)
                    {
                        t.Dispose();
                    }
                    Comment?.Dispose();
                }
                base.Dispose(disposing);
            }
        }
        public override void Release()
        {
            base.Release();
            if (Schema != null)
            {
                Schema.GetCollection(GetCollectionIndex())?.Invalidate();
            }
            foreach (Trigger t in Triggers)
            {
                t.Release();
            }
            Comment?.Release();
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
            int ret = Schema.Compare(Schema, sc.Schema);
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
