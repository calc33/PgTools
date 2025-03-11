using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Db2Source.PgsqlExtension;

namespace Db2Source
{
	public class PgsqlForeignDataWrapper : SchemaObject
	{
		public string Handler { get; set; }
		public string Validator { get; set; }
		public string[] Options { get; set; }

		public PgsqlForeignDataWrapper(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName, NamespaceIndex.ForeignDataWrapper) { }
		public PgsqlForeignDataWrapper(NamedCollection owner, PgsqlForeignDataWrapper basedOn) : base(owner, basedOn)
		{
			Handler = basedOn.Handler;
			Validator = basedOn.Validator;
			Options = basedOn.Options;
		}

		public override NamespaceIndex GetCollectionIndex()
		{
			return NamespaceIndex.ForeignDataWrapper;
		}

		public override string GetExportFolderName()
		{
			return "DBLink";
		}

		public override string GetSqlType()
		{
			return "DBLINK";
		}

		protected PgsqlForeignDataWrapper _backup = null;
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
			_backup = new PgsqlForeignDataWrapper(null, this);
		}
		protected internal void RestoreFrom(PgsqlForeignDataWrapper backup)
		{
			base.RestoreFrom(backup);
			Handler = backup.Handler;
			Validator = backup.Validator;
			if (backup.Options != null)
			{
				Options = new string[backup.Options.Length];
				Array.Copy(Options, backup.Options, Options.Length);
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
	}
}
