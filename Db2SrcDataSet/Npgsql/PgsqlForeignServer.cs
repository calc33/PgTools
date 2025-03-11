using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
	public class PgsqlForeignServer : SchemaObject
	{
		public string Fdw {  get; set; }
		public string ServerType { get; set; }
		public string Version { get; set; }
		public string[] Options { get; set; }

		public PgsqlForeignServer(Db2SourceContext context, string owner, string schema, string objectName) : base(context, owner, schema, objectName, NamespaceIndex.ForeignServer) { }
		public PgsqlForeignServer(NamedCollection owner, PgsqlForeignServer basedOn) : base(owner, basedOn)
		{
			Fdw = basedOn.Fdw;
			ServerType = basedOn.ServerType;
			Version = basedOn.Version;
			Options = basedOn.Options;
		}

		public override NamespaceIndex GetCollectionIndex()
		{
			return NamespaceIndex.ForeignServer;
		}

		public override string GetExportFolderName()
		{
			return "ForeignServer";
		}

		public override string GetSqlType()
		{
			return "FOREIGNSERVER";
		}

		protected PgsqlForeignServer _backup = null;
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
			_backup = new PgsqlForeignServer(null, this);
		}
		protected internal void RestoreFrom(PgsqlForeignServer backup)
		{
			base.RestoreFrom(backup);
			Fdw = backup.Fdw;
			ServerType = backup.ServerType;
			Version = backup.Version;
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
