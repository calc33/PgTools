﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class Tablespace : NamedObject
    {
        private string _name;
        private string _path;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value)
                {
                    return;
                }
                _name = value;
                OnPropertyChanged("Name");
            }
        }
        public string Path
        {
            get { return _path; }
            set
            {
                if (_path == value)
                {
                    return;
                }
                _path = value;
                OnPropertyChanged("Path");
            }
        }
        internal Tablespace _backup;
        protected override string GetIdentifier()
        {
            return Name;
        }

        public override void Backup()
        {
            _backup = new Tablespace(this);
        }
        public override void Restore()
        {
            if (_backup == null)
            {
                return;
            }
            Name = _backup.Name;
            Path = _backup.Path;
        }
        public override bool ContentEquals(NamedObject obj)
        {
            if (!base.ContentEquals(obj))
            {
                return false;
            }
            Tablespace ts = (Tablespace)obj;
            return Name == ts.Name
                && Path == ts.Path;
        }
        public override bool IsModified
        {
            get
            {
                return (_backup != null) && !ContentEquals(_backup);
            }
        }
        public Tablespace(NamedCollection owner) : base(owner) { }
        internal Tablespace(Tablespace basedOn) : base(null)
        {
            if (basedOn == null)
            {
                throw new ArgumentNullException("basedOn");
            }
            Name = basedOn.Name;
            Path = basedOn.Path;
        }
    }
}
