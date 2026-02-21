using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    [Flags]
    public enum CastContext
    {
        Assignment = 1,
        Implicit = 2,
    }
    public enum CastMethod
    {
        Function,
        BinaryCoercible,
        Inout,
    }
    public class Cast: NamedObject
    {
        public string SourceType { get; }
        public string TargetType { get; }
        public CastContext CastContext { get; }
        public CastMethod CastMethod { get; }
        public StoredFunction Function { get; }
        //public string FunctionOwner { get; }
        //public string FunctionName { get; }
        //public Cast(NamedCollection owner, string sourceType, string targetType, CastContext context, CastMethod method, string functionOwner, string functionName) : base(owner)
        //{
        //    SourceType = sourceType;
        //    TargetType = targetType;
        //    CastContext = context;
        //    CastMethod = method;
        //    FunctionOwner = functionOwner;
        //    FunctionName = functionName;
        //}
        public Cast(NamedCollection owner, string sourceType, string targetType, CastContext context, CastMethod method, StoredFunction function) : base(owner)
        {
            SourceType = sourceType;
            TargetType = targetType;
            CastContext = context;
            CastMethod = method;
            Function = function;
            //FunctionOwner = functionOwner;
            //FunctionName = functionName;
        }

        public override string ToString()
        {
            return string.Format("cast({0} as {1})", SourceType, TargetType);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Cast c))
            {
                return false;
            }
            return SourceType == c.SourceType && TargetType == c.TargetType;
        }

        public override int GetHashCode()
        {
            return SourceType.GetHashCode() * 13 + TargetType.GetHashCode();
        }

        protected override string GetFullIdentifier()
        {
            return string.Format("cast({0} as {1})", SourceType, TargetType);
        }

        protected override string GetIdentifier()
        {
            return string.Format("cast({0} as {1})", SourceType, TargetType);
        }

        protected override int GetIdentifierDepth()
        {
            return 1;
        }

        public override NamespaceIndex GetCollectionIndex()
        {
            return NamespaceIndex.Casts;
        }

        public string GetExportFolderName()
        {
            return "Cast";
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
    }
}
