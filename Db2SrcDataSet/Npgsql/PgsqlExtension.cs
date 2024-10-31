using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public class PgsqlExtension: SchemaObject
    {
        public class ConfigTable
        {
            public Table Table { get; set; }
            public string Condition { get; set; }
            public ConfigTable(Table table, string condition)
            {
                Table = table;
                Condition = condition;
            }
        }
        public bool IsHidden { get; set; }
        public bool Relocatable { get; set; }
        public string Version { get; set; }
        public string DefaultVersion { get; set; }
        public ConfigTable[] Configurations { get; set; }

        public PgsqlExtension(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName, NamespaceIndex.Extension) { }
        public PgsqlExtension(NamedCollection owner, PgsqlExtension basedOn) : base(owner, basedOn)
        {
            Relocatable = basedOn.Relocatable;
            Version = basedOn.Version;
            Configurations = new ConfigTable[basedOn.Configurations.Length];
            for (int i = 0; i < basedOn.Configurations.Length; i++)
            {
                Configurations[i] = basedOn.Configurations[i];
            }
        }

		public override NamespaceIndex GetCollectionIndex()
		{
			return NamespaceIndex.Extension;
		}
		
        protected override string GetFullIdentifier()
        {
            return Name;
        }
        public override string GetSqlType()
        {
            return "EXTENSION";
        }

        public override string GetExportFolderName()
        {
            return "Extension";
        }

		protected PgsqlExtension _backup = null;
        public override bool HasBackup()
        {
            return _backup != null;
        }

        public override void Backup(bool force)
        {
            if (!force && _backup != null)
            {
                return;
            }
            _backup = new PgsqlExtension(null, this);
        }

        protected internal void RestoreFrom(PgsqlExtension backup)
        {
            base.RestoreFrom(backup);
            Relocatable = backup.Relocatable;
            Version = backup.Version;
            Configurations = new ConfigTable[backup.Configurations.Length];
            for (int i = 0; i < backup.Configurations.Length; i++)
            {
                Configurations[i] = backup.Configurations[i];
            }

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
            PgsqlExtension ext = (PgsqlExtension)obj;
            return Relocatable == ext.Relocatable
                && Version == ext.Version
                && ArrayEquals(Configurations, ext.Configurations);
        }
        public override bool IsModified
        {
            get
            {
                return (_backup != null) && !ContentEquals(_backup);
            }
        }
    }
}
