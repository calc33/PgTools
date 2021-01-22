using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class Tablespace : NamedObject, ICloneable
    {
        public string Name { get; set; }
        public string Path { get; set; }
        protected override string GetIdentifier()
        {
            return Name;
        }

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        public Tablespace(NamedCollection owner) : base(owner) { }
    }
}
