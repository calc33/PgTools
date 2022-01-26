using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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

        //public abstract string InfoText { get; }
        public abstract NameValue[] Infos { get; }

        //public TypeReferenceCollection ReferFrom { get; } = new TypeReferenceCollection();

        internal Type_(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName, Schema.CollectionIndex.Objects) { }
        internal Type_(NamedCollection owner, Type_ basedOn) : base(owner, basedOn) { }
    }

    //public class TypeReference
    //{
    //    public SchemaObject Target { get; set; }
    //    public string Name { get; set; }

    //    public TypeReference(SchemaObject target, string name)
    //    {
    //        Target = target;
    //        Name = name;
    //    }
    //    public TypeReference(Column column)
    //    {
    //        Target = column.Table;
    //        Name = column.Name;
    //    }
    //}

    //public sealed class TypeReferenceCollection: IList<TypeReference>, IList
    //{
    //    private List<TypeReference> _list = new List<TypeReference>();

    //    public TypeReference this[int index]
    //    {
    //        get
    //        {
    //            return _list[index];
    //        }
    //        set
    //        {
    //            _list[index] = value;
    //        }
    //    }
    //    object IList.this[int index]
    //    {
    //        get
    //        {
    //            return ((IList)_list)[index];
    //        }
    //        set
    //        {
    //            ((IList)_list)[index] = value;
    //        }
    //    }

    //    public int Count
    //    {
    //        get
    //        {
    //            return _list.Count;
    //        }
    //    }

    //    public bool IsReadOnly
    //    {
    //        get
    //        {
    //            return ((IList<TypeReference>)_list).IsReadOnly;
    //        }
    //    }

    //    public object SyncRoot
    //    {
    //        get
    //        {
    //            return ((IList)_list).SyncRoot;
    //        }
    //    }

    //    public bool IsSynchronized
    //    {
    //        get
    //        {
    //            return ((IList)_list).IsSynchronized;
    //        }
    //    }

    //    public bool IsFixedSize
    //    {
    //        get
    //        {
    //            return ((IList)_list).IsFixedSize;
    //        }
    //    }

    //    public void Add(TypeReference item)
    //    {
    //        _list.Add(item);
    //    }

    //    public int Add(object value)
    //    {
    //        return ((IList)_list).Add(value);
    //    }

    //    public void Clear()
    //    {
    //        _list.Clear();
    //    }

    //    public bool Contains(TypeReference item)
    //    {
    //        return _list.Contains(item);
    //    }

    //    public bool Contains(object value)
    //    {
    //        return ((IList)_list).Contains(value);
    //    }

    //    public void CopyTo(TypeReference[] array, int arrayIndex)
    //    {
    //        _list.CopyTo(array, arrayIndex);
    //    }

    //    public void CopyTo(Array array, int index)
    //    {
    //        ((IList)_list).CopyTo(array, index);
    //    }

    //    public IEnumerator<TypeReference> GetEnumerator()
    //    {
    //        return _list.GetEnumerator();
    //    }

    //    public int IndexOf(TypeReference item)
    //    {
    //        return _list.IndexOf(item);
    //    }

    //    public int IndexOf(object value)
    //    {
    //        return ((IList)_list).IndexOf(value);
    //    }

    //    public void Insert(int index, TypeReference item)
    //    {
    //        _list.Insert(index, item);
    //    }

    //    public void Insert(int index, object value)
    //    {
    //        ((IList)_list).Insert(index, value);
    //    }

    //    public bool Remove(TypeReference item)
    //    {
    //        return _list.Remove(item);
    //    }

    //    public void Remove(object value)
    //    {
    //        ((IList)_list).Remove(value);
    //    }

    //    public void RemoveAt(int index)
    //    {
    //        _list.RemoveAt(index);
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return ((IEnumerable)_list).GetEnumerator();
    //    }
    //}
}
