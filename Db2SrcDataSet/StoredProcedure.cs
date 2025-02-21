using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
	public partial class StoredProcedure : StoredProcedureBase
	{
		private string _headerDef = null;
		protected override string RequireHeaderDef()
		{
			if (_headerDef != null)
			{
				return _headerDef;
			}
			_headerDef = Name + Parameters.GetParamDefText("(", ", ", ")");
			return _headerDef;
		}

		public StoredProcedure(Db2SourceContext context, string owner, string schema, string objectName, string definition, bool isLoaded) : base(context, owner, schema, objectName, definition, isLoaded)
		{
		}
	}
}
