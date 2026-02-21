using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class Operator : NamedObject
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public SchemaObject LeftType { get; set; }    // nullable
        public SchemaObject RightType { get; set; }
        public SchemaObject ResultType { get; set; }
        public string Commutator { get; set; }
        public string Negator { get; set; }
        public StoredFunction RestrictProc {  get; set; }
        public StoredFunction JoinProc { get; set; }
        public bool HashEnabled { get; set; }
        public bool MergeEnabled { get; set; }

        public override void Backup(bool force)
        {
            throw new NotImplementedException();
        }

        public override NamespaceIndex GetCollectionIndex()
        {
            throw new NotImplementedException();
        }

        public override bool HasBackup()
        {
            return false;
        }

        public override void Restore()
        {
            throw new NotImplementedException();
        }

        protected override string GetFullIdentifier()
        {
            if (LeftType is null)
            {
                return string.Format("{0}.{1}({2})", Schema, Name, RightType.FullIdentifier);
            }
            else
            {
                return string.Format("{0}.{1}({2},{3})", Schema, Name, LeftType.FullIdentifier, RightType.FullIdentifier);
            }
        }

        protected override string GetIdentifier()
        {
            if (LeftType is null)
            {
                return string.Format("{0}({1})", Name, RightType.FullIdentifier);
            }
            else
            {
                return string.Format("{0}({1},{2})", Name, LeftType.FullIdentifier, RightType.FullIdentifier);
            }
        }

        protected override int GetIdentifierDepth()
        {
            return 1;
        }

        public Operator(NamedCollection owner) : base(owner)
        {
        }
    }
}
