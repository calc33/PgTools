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

        /// <summary>
        /// ユーザーのID(文字列)
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// ユーザーの名前(フルネーム)
        /// </summary>
        public string Name { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// trueの場合 Passwordには有効な値が入っていない
        /// </summary>
        public bool IsPasswordShadowed { get; set; }
        /// <summary>
        /// パスワード有効期限
        /// </summary>
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
