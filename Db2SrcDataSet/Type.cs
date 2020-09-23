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

        public TypeReference this[int index] { get => _list[index]; set => _list[index] = value; }
        object IList.this[int index] { get => ((IList)_list)[index]; set => ((IList)_list)[index] = value; }

        public int Count => _list.Count;

        public bool IsReadOnly => ((IList<TypeReference>)_list).IsReadOnly;

        public object SyncRoot => ((IList)_list).SyncRoot;

        public bool IsSynchronized => ((IList)_list).IsSynchronized;

        public bool IsFixedSize => ((IList)_list).IsFixedSize;

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
        internal BasicType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
    }

    public class EnumType : Type_
    {
        public string[] Labels { get; set; }
        internal EnumType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
    }

    public class RangeType : Type_
    {
        public string Subtype { get; set; }
        public string SubtypeOpClass { get; set; }
        public string Collation { get; set; }
        public string CanonicalFunction { get; set; }
        public string SubtypeDiff { get; set; }
        internal RangeType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
    }
}
