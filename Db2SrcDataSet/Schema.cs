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
            Objects = 0,
            Constraints = 1,
            Columns = 2,
            Comments = 3,
            Indexes = 4,
            Triggers = 5,
            //Procedures = 6,
        }
        public Db2SourceContext Owner { get; private set; }
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
        private bool? _isHidden = null;
        public bool IsHidden
        {
            get
            {
                if (!_isHidden.HasValue)
                {
                    _isHidden = Owner.IsHiddenSchema(Name);
                }
                return _isHidden.Value;
            }
        }
        private NamedCollection[] _collections;
        public NamedCollection GetCollection(CollectionIndex index)
        {
            return _collections[(int)index];
        }
        public NamedCollection<SchemaObject> Objects { get; } = new NamedCollection<SchemaObject>();
        public NamedCollection<Column> Columns { get; } = new NamedCollection<Column>();
        public NamedCollection<Comment> Comments { get; } = new NamedCollection<Comment>();
        public NamedCollection<Constraint> Constraints { get; } = new NamedCollection<Constraint>();
        public NamedCollection<Index> Indexes { get; } = new NamedCollection<Index>();
        public NamedCollection<Trigger> Triggers { get; } = new NamedCollection<Trigger>();
        //public NamedCollection<StoredFunction> Procedures { get; } = new NamedCollection<StoredFunction>();
        internal Schema(Db2SourceContext owner, string name) : base(owner.Schemas)
        {
            Owner = owner;
            Name = name;
            _collections = new NamedCollection[] { Objects, Constraints, Columns, Comments, Indexes, Triggers /*, Procedures */ };
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

        public override bool Equals(object obj)
        {
            if (!(obj is Schema))
            {
                return false;
            }
            Schema sc = (Schema)obj;
            return ((Owner != null) ? Owner.Equals(sc.Owner) : (Owner == sc.Owner)) && (Name == sc.Name);
        }

        public override int GetHashCode()
        {
            return ((Owner != null) ? Owner.GetHashCode() : 0) + (string.IsNullOrEmpty(Name) ? 0 : Name.GetHashCode());
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
            if (Owner == null)
            {
                ret = (sc.Owner == null) ? 0 : 1;
            }
            else
            {
                ret = Owner.CompareTo(sc.Owner);
            }
            return ret;
        }
    }
}
