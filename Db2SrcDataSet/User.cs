using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class User : NamedObject, ICloneable
    {
        public User(NamedCollection owner) : base(owner) { }

        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime PasswordExpiration { get; set; } = DateTime.MaxValue;

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        protected override string GetIdentifier()
        {
            return Id;
        }
    }
}
