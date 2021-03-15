using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlBasicType : Type_
    {
        public string InputFunction { get; set; }
        public string OutputFunction { get; set; }
        public string ReceiveFunction { get; set; }
        public string SendFunction { get; set; }
        public string TypmodInFunction { get; set; }
        public string TypmodOutFunction { get; set; }
        public string AnalyzeFunction { get; set; }
        public int InternalLength { get; set; }
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

        protected internal PgsqlBasicType _backup;
        public override void Backup()
        {
            _backup = new PgsqlBasicType(this);
        }
        protected internal void RestoreFrom(PgsqlBasicType backup)
        {
            base.RestoreFrom(backup);
            InputFunction = backup.InputFunction;
            OutputFunction = backup.OutputFunction;
            ReceiveFunction = backup.ReceiveFunction;
            SendFunction = backup.SendFunction;
            TypmodInFunction = backup.TypmodInFunction;
            TypmodOutFunction = backup.TypmodOutFunction;
            AnalyzeFunction = backup.AnalyzeFunction;
            InternalLength = backup.InternalLength;
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
            PgsqlBasicType t = (PgsqlBasicType)obj;
            return InputFunction == t.InputFunction
                && OutputFunction == t.OutputFunction
                && ReceiveFunction == t.ReceiveFunction
                && SendFunction == t.SendFunction
                && TypmodInFunction == t.TypmodInFunction
                && TypmodOutFunction == t.TypmodOutFunction
                && AnalyzeFunction == t.AnalyzeFunction
                && InternalLength == t.InternalLength
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

        public override NameValue[] Infos
        {
            get
            {
                return new NameValue[]
                {
                    new NameValue() { Name = "Input", Value = InputFunction },
                    new NameValue() { Name = "Output", Value = OutputFunction },
                    new NameValue() { Name = "Receive", Value = ReceiveFunction },
                    new NameValue() { Name = "Send", Value = SendFunction },
                    new NameValue() { Name = "Typmod_IN", Value = TypmodInFunction },
                    new NameValue() { Name = "Typmod_OUT", Value = TypmodOutFunction },
                    new NameValue() { Name = "Analyze", Value = AnalyzeFunction },
                    new NameValue() { Name = "InternalLength", Value = InternalLength == -1 ? "variable" : InternalLength.ToString() },
                    new NameValue() { Name = "PassedbyValue", Value = PassedbyValue ? "true" : "false" },
                    new NameValue() { Name = "Alignment", Value = Alignment.ToString() },
                    new NameValue() { Name = "Storage", Value = Storage },
                    new NameValue() { Name = "Category", Value = Category },
                    new NameValue() { Name = "Preferred", Value = Preferred ? "true" : "false" },
                    new NameValue() { Name = "Default", Value = Default },
                    new NameValue() { Name = "Element", Value = Element },
                    new NameValue() { Name = "Delimiter", Value = Delimiter },
                    new NameValue() { Name = "Collatable", Value = Collatable ? "true" : "false" }
                };
            }
        }
        internal PgsqlBasicType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
        internal PgsqlBasicType(PgsqlBasicType basedOn) : base(basedOn)
        {
            InputFunction = basedOn.InputFunction;
            OutputFunction = basedOn.OutputFunction;
            ReceiveFunction = basedOn.ReceiveFunction;
            SendFunction = basedOn.SendFunction;
            TypmodInFunction = basedOn.TypmodInFunction;
            TypmodOutFunction = basedOn.TypmodOutFunction;
            AnalyzeFunction = basedOn.AnalyzeFunction;
            InternalLength = basedOn.InternalLength;
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

    public class PgsqlEnumType : Type_
    {
        public string[] Labels { get; set; }

        protected internal PgsqlEnumType _backup;
        public override void Backup()
        {
            _backup = new PgsqlEnumType(this);
        }
        protected internal void RestoreFrom(PgsqlEnumType backup)
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
            PgsqlEnumType t = (PgsqlEnumType)obj;
            return ArrayEquals(Labels, t.Labels);
        }
        public override bool IsModified()
        {
            return (_backup != null) && !ContentEquals(_backup);
        }
        public override NameValue[] Infos
        {
            get
            {
                List<NameValue> l = new List<NameValue>();
                foreach (string s in Labels)
                {
                    l.Add(new NameValue() { Name = s, Value = null });
                }
                return l.ToArray();
            }
        }
        internal PgsqlEnumType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
        internal PgsqlEnumType(PgsqlEnumType basedOn) : base(basedOn)
        {
            Labels = (string[])basedOn.Labels.Clone();
        }
    }

    public class PgsqlRangeType : Type_
    {
        public string Subtype { get; set; }
        public string SubtypeOpClass { get; set; }
        public string Collation { get; set; }
        public string CanonicalFunction { get; set; }
        public string SubtypeDiff { get; set; }

        protected internal PgsqlRangeType _backup;
        public override void Backup()
        {
            _backup = new PgsqlRangeType(this);
        }
        protected internal void RestoreFrom(PgsqlRangeType backup)
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
            PgsqlRangeType t = (PgsqlRangeType)obj;
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
        
        public override NameValue[] Infos
        {
            get
            {
                return new NameValue[]
                {
                    new NameValue() { Name = "SubType", Value = Subtype },
                    new NameValue() { Name = "SubTypeOpClass", Value = SubtypeOpClass },
                    new NameValue() { Name = "Collation", Value = Collation },
                    new NameValue() { Name = "Canonical", Value = CanonicalFunction },
                    new NameValue() { Name = "SubtypeDiff", Value = SubtypeDiff }
                };
            }
        }

        internal PgsqlRangeType(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName) { }
        internal PgsqlRangeType(PgsqlRangeType basedOn) : base(basedOn)
        {
            Subtype = basedOn.Subtype;
            SubtypeOpClass = basedOn.SubtypeOpClass;
            Collation = basedOn.Collation;
            CanonicalFunction = basedOn.CanonicalFunction;
            SubtypeDiff = basedOn.SubtypeDiff;
        }
    }
}
