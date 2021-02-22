using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class User : NamedObject
    {
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
        protected User _backup;

        public override void Backup()
        {
            _backup = new User(this);
        }
        
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            Id = _backup.Id;
            Name = _backup.Name;
            Password = _backup.Password;
            IsPasswordShadowed = _backup.IsPasswordShadowed;
            PasswordExpiration = _backup.PasswordExpiration;
        }

        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            User u = (User)obj;
            return Id == u.Id
                && Name == u.Name
                && Password == u.Password
                && IsPasswordShadowed == u.IsPasswordShadowed
                && PasswordExpiration == u.PasswordExpiration;
        }
        protected override string GetIdentifier()
        {
            return Id;
        }

        public User(NamedCollection owner) : base(owner) { }
        internal User(User basedOn) : base(null)
        {
            if (basedOn == null)
            {
                throw new ArgumentNullException("basedOn");
            }
            Id = basedOn.Id;
            Name = basedOn.Name;
            Password = basedOn.Password;
            IsPasswordShadowed = basedOn.IsPasswordShadowed;
            PasswordExpiration = basedOn.PasswordExpiration;
        }
    }
}