using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class Schema : NamedObject, IComparable
    {
        public enum CollectionIndex
        {
            None = 0,
            Objects = 1,
            Constraints = 2,
            Columns = 3,
            Comments = 4,
            Indexes = 5,
            Triggers = 6,
            Sequences = 7,
            //Procedures = 8,
        }
        public Db2SourceContext Context { get; private set; }

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            private set
            {
                if (_name == value)
                {
                    return;
                }
                _name = value;
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

        private bool? _isHidden = null;
        public bool IsHidden
        {
            get
            {
                if (!_isHidden.HasValue)
                {
                    _isHidden = Context.IsHiddenSchema(Name);
                }
                return _isHidden.Value;
            }
        }

        private NamedCollection[] _collections;
        public NamedCollection GetCollection(CollectionIndex index)
        {
            return _collections[(int)index];
        }
        //public NamedCollection<SchemaObject> Nones { get; } = new NamedCollection<SchemaObject>();
        public NamedCollection<SchemaObject> Objects { get; } = new NamedCollection<SchemaObject>();
        public NamedCollection<Column> Columns { get; } = new NamedCollection<Column>();
        public NamedCollection<Comment> Comments { get; } = new NamedCollection<Comment>();
        public NamedCollection<Constraint> Constraints { get; } = new NamedCollection<Constraint>();
        public NamedCollection<Index> Indexes { get; } = new NamedCollection<Index>();
        public NamedCollection<Trigger> Triggers { get; } = new NamedCollection<Trigger>();
        public NamedCollection<Sequence> Sequences { get; } = new NamedCollection<Sequence>();
        //public NamedCollection<StoredFunction> Procedures { get; } = new NamedCollection<StoredFunction>();
        internal Schema(Db2SourceContext owner, string name) : base(owner.Schemas)
        {
            Context = owner;
            Name = name;
            _collections = new NamedCollection[] { null, Objects, Constraints, Columns, Comments, Indexes, Triggers, Sequences /*, Procedures */ };
        }

        protected override string GetIdentifier()
        {
            return Name;
        }
        public void InvalidateColumns()
        {
            foreach (SchemaObject o in Objects)
            {
                o.InvalidateColumns();
            }
        }

        public void InvalidateConstraints()
        {
            foreach (SchemaObject o in Objects)
            {
                (o as Table)?.InvalidateConstraints();
            }
        }

        public void InvalidateTriggers()
        {
            foreach (SchemaObject o in Objects)
            {
                o.InvalidateTriggers();
            }
        }

        public void InvalidateIndexes()
        {
            foreach (SchemaObject o in Objects)
            {
                (o as Table)?.InvalidateIndexes();
            }
        }
        //public void InvalidateSequences()
        //{
        //    foreach (SchemaObject o in Objects)
        //    {
        //        (o as Table)?.invalidateSequences();
        //    }
        //}

        private Dictionary<string, string[]> _storedProcToTrigger = null;

        internal void InvalidateStoredProcToTrigger()
        {
            _storedProcToTrigger = null;
        }
        internal void UpdateStoredProcToTrigger()
        {
            if (_storedProcToTrigger != null)
            {
                return;
            }
            Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
            foreach (Trigger t in Triggers)
            {
                string key = t.ProcedureSchema + "." + t.ProcedureName;
                List<string> l;
                if (!dict.TryGetValue(key, out l))
                {
                    l = new List<string>();
                    dict.Add(key, l);
                }
                string v = t.Name + "@" + t.Owner;
                l.Add(v);
            }
            _storedProcToTrigger = new Dictionary<string, string[]>();
            foreach (var kv in dict)
            {
                kv.Value.Sort();
                _storedProcToTrigger.Add(kv.Key, kv.Value.ToArray());
            }
        }

        private static readonly Trigger[] NoTrigger = new Trigger[0];
        public Trigger[] GetCallingTriggers(StoredFunction triggerFunction)
        {
            UpdateStoredProcToTrigger();
            string[] triggerNames;
            if (!_storedProcToTrigger.TryGetValue(triggerFunction.Identifier, out triggerNames))
            {
                return NoTrigger;
            }
            List<Trigger> l = new List<Trigger>();
            foreach (string s in triggerNames)
            {
                Trigger tr = Triggers[s];
                if (tr != null)
                {
                    l.Add(tr);
                }
            }
            return l.ToArray();
        }

        public override void Backup()
        {
            throw new NotImplementedException();
        }
        public override void Restore()
        {
            throw new NotImplementedException();
        }
        public override bool IsModified { get { return false; } }
        public override bool Equals(object obj)
        {
            if (!(obj is Schema))
            {
                return false;
            }
            Schema sc = (Schema)obj;
            return ((Context != null) ? Context.Equals(sc.Context) : (Context == sc.Context)) && (Name == sc.Name);
        }

        public override int GetHashCode()
        {
            return ((Context != null) ? Context.GetHashCode() : 0) * 13 + (string.IsNullOrEmpty(Name) ? 0 : Name.GetHashCode());
        }

        public override string ToString()
        {
            return Name;
        }

        public override int CompareTo(object obj)
        {
            if (!(obj is Schema))
            {
                return -1;
            }
            Schema sc = (Schema)obj;
            int ret = (IsHidden ? 1 : 0) - (sc.IsHidden ? 1 : 0);
            if (ret != 0)
            {
                return ret;
            }
            ret = string.Compare(Name, sc.Name);
            if (ret != 0)
            {
                return ret;
            }
            if (Context == null)
            {
                ret = (sc.Context == null) ? 0 : 1;
            }
            else
            {
                ret = Context.CompareTo(sc.Context);
            }
            return ret;
        }
        public static int Compare(Schema obj1, Schema obj2)
        {
            if (obj1 == null)
            {
                return (obj2 != null) ? 1 : 0;
            }
            return obj1.CompareTo(obj2);
        }
    }
}
