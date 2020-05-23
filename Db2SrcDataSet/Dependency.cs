using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Db2Source
{
    //public class Dependency
    //{
    //    public SchemaObject From { get; private set; }
    //    public SchemaObject To { get; private set; }
    //    public Dependency(SchemaObject from, SchemaObject to)
    //    {
    //        From = from;
    //        To = to;
    //    }
    //}

    //public sealed class DependencyCollection : IList<Dependency>
    //{
    //    private List<Dependency> _dependencies = new List<Dependency>();
    //    private Dictionary<SchemaObject, IList<SchemaObject>> _dependFrom = null;
    //    private Dictionary<SchemaObject, IList<SchemaObject>> _dependTo = null;
    //    private void InvalidateDependencies()
    //    {
    //        _dependFrom = null;
    //        _dependTo = null;
    //    }
    //    private void UpdateDependencies()
    //    {
    //        if (_dependFrom != null && _dependTo != null)
    //        {
    //            return;
    //        }
    //        Dictionary<SchemaObject, IList<SchemaObject>> from = new Dictionary<SchemaObject, IList<SchemaObject>>();
    //        Dictionary<SchemaObject, IList<SchemaObject>> to = new Dictionary<SchemaObject, IList<SchemaObject>>();
    //        IList<SchemaObject> l;
    //        foreach (Dependency d in _dependencies)
    //        {
    //            l = null;
    //            if (!from.TryGetValue(d.From, out l))
    //            {
    //                l = new List<SchemaObject>();
    //                from.Add(d.From, l);
    //            }
    //            l.Add(d.To);
    //            l = null;
    //            if (!to.TryGetValue(d.To, out l))
    //            {
    //                l = new List<SchemaObject>();
    //                to.Add(d.To, l);
    //            }
    //            l.Add(d.From);
    //        }
    //        foreach (SchemaObject o in from.Keys)
    //        {
    //            l = from[o];
    //            ((List<SchemaObject>)l).Sort();
    //            from[o] = l.ToArray();
    //        }
    //        foreach (SchemaObject o in to.Keys)
    //        {
    //            l = to[o];
    //            ((List<SchemaObject>)l).Sort();
    //            to[o] = l.ToArray();
    //        }
    //        _dependFrom = from;
    //        _dependTo = to;
    //    }
    //    private static SchemaObject[] EmptySchemaObjectArray = new SchemaObject[0];

    //    public SchemaObject[] GetDepended(SchemaObject target)
    //    {
    //        UpdateDependencies();
    //        IList<SchemaObject> l;
    //        if (_dependFrom.TryGetValue(target, out l))
    //        {
    //            return l as SchemaObject[];
    //        }
    //        return EmptySchemaObjectArray;
    //    }
    //    public SchemaObject[] GetDependOn(SchemaObject target)
    //    {
    //        UpdateDependencies();
    //        IList<SchemaObject> l;
    //        if (_dependTo.TryGetValue(target, out l))
    //        {
    //            return l as SchemaObject[];
    //        }
    //        return EmptySchemaObjectArray;
    //    }

    //    public DependencyCollection() { }
    //    public DependencyCollection(IEnumerable<Dependency> dependencies)
    //    {
    //        _dependencies = new List<Dependency>(dependencies);
    //    }

    //    #region IList<SchemaObject>の実装
    //    public int Count { get { return (_dependencies).Count; } }

    //    public bool IsReadOnly { get { return false; } }

    //    public Dependency this[int index]
    //    {
    //        get { return _dependencies[index]; }
    //        set
    //        {
    //            if (_dependencies[index] == value)
    //            {
    //                return;
    //            }
    //            _dependencies[index] = value;
    //            InvalidateDependencies();
    //        }
    //    }
    //    public int IndexOf(Dependency item)
    //    {
    //        return _dependencies.IndexOf(item);
    //    }

    //    public void Insert(int index, Dependency item)
    //    {
    //        _dependencies.Insert(index, item);
    //        InvalidateDependencies();
    //    }

    //    public void RemoveAt(int index)
    //    {
    //        _dependencies.RemoveAt(index);
    //        InvalidateDependencies();
    //    }

    //    public void Add(Dependency item)
    //    {
    //        _dependencies.Add(item);
    //        InvalidateDependencies();
    //    }

    //    public void Clear()
    //    {
    //        _dependencies.Clear();
    //        InvalidateDependencies();
    //    }

    //    public bool Contains(Dependency item)
    //    {
    //        return _dependencies.Contains(item);
    //    }

    //    public void CopyTo(Dependency[] array, int arrayIndex)
    //    {
    //        _dependencies.CopyTo(array, arrayIndex);
    //    }

    //    public bool Remove(Dependency item)
    //    {
    //        bool flag = _dependencies.Remove(item);
    //        if (flag)
    //        {
    //            InvalidateDependencies();
    //        }
    //        return flag;
    //    }

    //    public IEnumerator<Dependency> GetEnumerator()
    //    {
    //        return _dependencies.GetEnumerator();
    //    }

    //    IEnumerator IEnumerable.GetEnumerator()
    //    {
    //        return _dependencies.GetEnumerator();
    //    }
    //    #endregion
    //}
}
