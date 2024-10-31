using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class Schema : NamedObject, IComparable
    {
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

        internal Schema(Db2SourceContext context, string name) : base(context.Schemas)
        {
            Context = context;
            Name = name;
        }

		public override NamespaceIndex GetCollectionIndex()
		{
			return NamespaceIndex.Schemas;
		}
		
        protected override string GetFullIdentifier()
        {
            return Name;
        }
        protected override string GetIdentifier()
        {
            return Name;
        }
        protected override int GetIdentifierDepth()
        {
            return 1;
        }

        public override bool HasBackup()
        {
            return false;
        }

        public override void Backup(bool force)
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
