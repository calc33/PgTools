using System;
using System.Collections;
using System.Collections.Generic;

namespace Db2Source
{
    public abstract class Type_: SchemaObject
    {
        public override string GetSqlType()
        {
            return "TYPE";
        }
        public override string GetExportFolderName()
        {
            return "Type";
        }

        public TypeReferenceCollection ReferFrom { get; } = new TypeReferenceCollection();

        internal Type_(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName, Schema.CollectionIndex.Objects) { }
        internal Type_(Type_ basedOn) : base(basedOn) { }
    }

    public class TypeReference
    {
        public SchemaObject Target { get; set; }
        public string Name { get; set; }

        public TypeReference(SchemaObject target, string name)
        {
            Target = target;
            Name = name;
        }
        public TypeReference(Column column)
        {
            Target = column.Table;
            Name = column.Name;
        }
    }

    public sealed class TypeReferenceCollection: IList<TypeReference>, IList
    {
        private List<TypeReference> _list = new List<TypeReference>();

        public TypeReference this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                _list[index] = value;
            }
        }
        object IList.this[int index]
        {
            get
            {
                return ((IList)_list)[index];
            }
            set
            {
                ((IList)_list)[index] = value;
            }
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return ((IList<TypeReference>)_list).IsReadOnly;
            }
        }

        public object SyncRoot
        {
            get
            {
                return ((IList)_list).SyncRoot;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return ((IList)_list).IsSynchronized;
            }
        }

        public bool IsFixedSize
        {
            get
            {
                return ((IList)_list).IsFixedSize;
            }
        }

        public void Add(TypeReference item)
        {
            _list.Add(item);
        }

        public int Add(object value)
        {
            return ((IList)_list).Add(value);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(TypeReference item)
        {
            return _list.Contains(item);
        }

        public bool Contains(object value)
        {
            return ((IList)_list).Contains(value);
        }

        public void CopyTo(TypeReference[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)_list).CopyTo(array, index);
        }

        public IEnumerator<TypeReference> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(TypeReference item)
        {
            return _list.IndexOf(item);
        }

        public int IndexOf(object value)
        {
            return ((IList)_list).IndexOf(value);
        }

        public void Insert(int index, TypeReference item)
        {
            _list.Insert(index, item);
        }

        public void Insert(int index, object value)
        {
            ((IList)_list).Insert(index, value);
        }

        public bool Remove(TypeReference item)
        {
            return _list.Remove(item);
        }

        public void Remove(object value)
        {
            ((IList)_list).Remove(value);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }
    }

    public class BasicType : Type_
    {
        public string InputFunction { get; set; }
        public string OutputFunction { get; set; }
        public string ReceiveFunction { get; set; }
        public string SendFunction { get; set; }
        public string TypmodInFunction { get; set; }
        public string TypmodOutFunction { get; set; }
        public string AnalyzeFunction { get; set; }
        public string InternalLengthFunction { get; set; }
        public bool PassedbyValue { get; set; }
        public int Alignment { get; set; }
        public string Storage { get; set; }
        public string Like { get; set; }
        public string Category { get; set; }
        public bool Preferred { get; set; }
        public string Default { get; set; }
        public string Element { get; set; }
        public string Delimiter { get; set; }
        public bool Collatable { get; set; }

        protected internal BasicType _backup;
        public override void Backup()
        {
            _backup = new BasicType(this);
        }
        protected internal void RestoreFrom(BasicType backup)
        {
            base.RestoreFrom(backup);
            InputFunction = backup.InputFunction;
            OutputFunction = backup.OutputFunction;
            ReceiveFunction = backup.ReceiveFunction;
            SendFunction = backup.SendFunction;
            TypmodInFunction = backup.TypmodInFunction;
            TypmodOutFunction = backup.TypmodOutFunction;
            AnalyzeFunction = backup.AnalyzeFunction;
            InternalLengthFunction = backup.InternalLengthFunction;
            PassedbyValue = backup.PassedbyValue;
            Alignment = backup.Alignment;
            Storage = backup.Storage;
            Like = backup.Like;
            Category = backup.Category;
            Preferred = backup.Preferred;
            Default = backup.Default;
            Element = backup.Element;
            Delimiter = backup.Delimiter;
            Collatable = backup.Collatable;
        }
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            RestoreFrom(_backup);
        }
        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            BasicType t = (BasicType)obj;
            return InputFunction == t.InputFunction
                && OutputFunction == t.OutputFunction
                && ReceiveFunction == t.ReceiveFunction
                && SendFunction == t.SendFunction
                && TypmodInFunction == t.TypmodInFunction
                && TypmodOutFunction == t.TypmodOutFunction
                && AnalyzeFunction == t.AnalyzeFunction
                && InternalLengthFunction == t.InternalLengthFunction
                && PassedbyValue == t.PassedbyValue
                && Alignment == t.Alignment
                && Storage == t.Storage
                && Like == t.Like
                && Category == t.Category
                && Preferred == t.Preferred
                && Default == t.Default
                && Element == t.Element
                && Delimiter == t.Delimiter
                && Collatable == t.Collatable;
        }
        public override bool IsModified()
        {
            return (_backup != null) && !ContentEquals(_backup);
        }
        internal BasicType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
        internal BasicType(BasicType basedOn) : base(basedOn)
        {
            InputFunction = basedOn.InputFunction;
            OutputFunction = basedOn.OutputFunction;
            ReceiveFunction = basedOn.ReceiveFunction;
            SendFunction = basedOn.SendFunction;
            TypmodInFunction = basedOn.TypmodInFunction;
            TypmodOutFunction = basedOn.TypmodOutFunction;
            AnalyzeFunction = basedOn.AnalyzeFunction;
            InternalLengthFunction = basedOn.InternalLengthFunction;
            PassedbyValue = basedOn.PassedbyValue;
            Alignment = basedOn.Alignment;
            Storage = basedOn.Storage;
            Like = basedOn.Like;
            Category = basedOn.Category;
            Preferred = basedOn.Preferred;
            Default = basedOn.Default;
            Element = basedOn.Element;
            Delimiter = basedOn.Delimiter;
            Collatable = basedOn.Collatable;
        }
    }

    public class EnumType : Type_
    {
        public string[] Labels { get; set; }

        protected internal EnumType _backup;
        public override void Backup()
        {
            _backup = new EnumType(this);
        }
        protected internal void RestoreFrom(EnumType backup)
        {
            base.RestoreFrom(backup);
            Labels = (string[])backup.Labels.Clone();
        }
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            RestoreFrom(_backup);
        }
        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            EnumType t = (EnumType)obj;
            return ArrayEquals(Labels, t.Labels);
        }
        public override bool IsModified()
        {
            return (_backup != null) && !ContentEquals(_backup);
        }
        internal EnumType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
        internal EnumType(EnumType basedOn) : base(basedOn)
        {
            Labels = (string[])basedOn.Labels.Clone();
        }
    }

    public class RangeType : Type_
    {
        public string Subtype { get; set; }
        public string SubtypeOpClass { get; set; }
        public string Collation { get; set; }
        public string CanonicalFunction { get; set; }
        public string SubtypeDiff { get; set; }

        protected internal RangeType _backup;
        public override void Backup()
        {
            _backup = new RangeType(this);
        }
        protected internal void RestoreFrom(RangeType backup)
        {
            base.RestoreFrom(backup);
            Subtype = backup.Subtype;
            SubtypeOpClass = backup.SubtypeOpClass;
            Collation = backup.Collation;
            CanonicalFunction = backup.CanonicalFunction;
            SubtypeDiff = backup.SubtypeDiff;
        }
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            RestoreFrom(_backup);
        }
        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            RangeType t = (RangeType)obj;
            return Subtype == t.Subtype
                && SubtypeOpClass == t.SubtypeOpClass
                && Collation == t.Collation
                && CanonicalFunction == t.CanonicalFunction
                && SubtypeDiff == t.SubtypeDiff;
        }
        public override bool IsModified()
        {
            return (_backup != null) && !ContentEquals(_backup);
        }
        internal RangeType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
        internal RangeType(RangeType basedOn):base(basedOn)
        {
            Subtype = basedOn.Subtype;
            SubtypeOpClass = basedOn.SubtypeOpClass;
            Collation = basedOn.Collation;
            CanonicalFunction = basedOn.CanonicalFunction;
            SubtypeDiff = basedOn.SubtypeDiff;
        }
    }
}
