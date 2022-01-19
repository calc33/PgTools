using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlDatabase : Database
    {
        private string _lcCollate;
        private string _lcCtype;
        private int? _connectionLimit;
        private bool _allowConnect;
        private bool _isTemplate;
        public string LcCollate
        {
            get
            {
                return _lcCollate;
            }
            set
            {
                if (_lcCollate == value)
                {
                    return;
                }
                _lcCollate = value;
                OnPropertyChanged("LcCollate");
            }
        }

        public string LcCtype
        {
            get
            {
                return _lcCtype;
            }
            set
            {
                if (_lcCtype == value)
                {
                    return;
                }
                _lcCtype = value;
                OnPropertyChanged("LcCtype");
            }
        }

        public int? ConnectionLimit
        {
            get
            {
                return _connectionLimit;
            }
            set
            {
                if (_connectionLimit == value)
                {
                    return;
                }
                _connectionLimit = value;
                OnPropertyChanged("ConnectionLimit");
            }
        }
        public bool AllowConnect
        {
            get
            {
                return _allowConnect;
            }
            set
            {
                if (_allowConnect == value)
                {
                    return;
                }
                _allowConnect = value;
                OnPropertyChanged("AllowConnect");
            }
        }
        public bool IsTemplate
        {
            get
            {
                return _isTemplate;
            }
            set
            {
                if (_isTemplate == value)
                {
                    return;
                }
                _isTemplate = value;
                OnPropertyChanged("IsTemplate");
            }
        }

        public override bool IsEnabled
        {
            get
            {
                return !IsTemplate;
            }
        }

        public PgsqlSettingCollection Settings { get; private set; } = new PgsqlSettingCollection();

        public override void Backup()
        {
            _backup = new PgsqlDatabase(this);
        }
        protected void RestoreFrom(PgsqlDatabase backup)
        {
            base.RestoreFrom(backup);
            _lcCollate = backup.LcCollate;
            _lcCtype = backup.LcCtype;
            _connectionLimit = backup.ConnectionLimit;
            _allowConnect = backup.AllowConnect;
            _isTemplate = backup.IsTemplate;
            Settings = (PgsqlSettingCollection)backup.Settings.Clone();
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
            PgsqlDatabase db = (PgsqlDatabase)obj;
            return Settings.ContentEquals(db.Settings);
        }

        private int GetPriorityByTemplateName()
        {
            if (!IsTemplate)
            {
                return 999;
            }
            switch (Name)
            {
                case "template1":
                    return 1;
                case "template0":
                    return 2;
                default:
                    return 999;
            }
        }

        /// <summary>
        /// テンプレートの並べ替えの際には"template1"と"template0"だけ特別扱いする
        /// template1を先頭に、template0を二番目に、それ以降はテンプレートを優先しつつ名前順
        /// </summary>
        /// <returns></returns>
        public static int CompareTemplate(PgsqlDatabase x, PgsqlDatabase y)
        {
            int ret;
            if (x == null || y == null)
            {
                return (x == null ? 1 : 0) - (y == null ? 1 : 0);
            }
            ret = x.GetPriorityByTemplateName() - y.GetPriorityByTemplateName();
            if (ret != 0)
            {
                return ret;
            }
            ret = (x.IsTemplate ? 0 : 1) - (y.IsTemplate ? 0 : 1);
            if (ret != 0)
            {
                return ret;
            }
            return x.CompareTo(y);
        }


        public PgsqlDatabase(Db2SourceContext context, string objectName) : base(context, objectName) { }
        public PgsqlDatabase(PgsqlDatabase basedOn) : base(basedOn)
        {
            _lcCollate = basedOn.LcCollate;
            _lcCtype = basedOn.LcCtype;
            _connectionLimit = basedOn.ConnectionLimit;
            _allowConnect = basedOn.AllowConnect;
            _isTemplate = basedOn.IsTemplate;
            Settings = (PgsqlSettingCollection)basedOn.Settings.Clone();
        }
    }
}
