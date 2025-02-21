using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace Db2Source
{

    public interface IReturnType
    {
        string GetSQL(Db2SourceContext context, string prefix, int indent, int charPerLine);
        string GetDefName();
    }

    public class SimpleReturnType : IReturnType
    {
        public string DataType { get; set; }

        public string GetSQL(Db2SourceContext context, string prefix, int indent, int charPerLine)
        {
            return prefix + DataType;
        }

        public string GetDefName()
        {
            return DataType;
        }

        public SimpleReturnType() { }
        public SimpleReturnType(string dataType)
        {
            DataType = dataType;
        }
    }

    public partial class StoredFunction: StoredProcedureBase/*, IDbTypeDef*/
    {
        public IReturnType ReturnType { get; set; }

        private string _headerDef = null;
        protected override string RequireHeaderDef()
        {
            if (_headerDef != null)
            {
                return _headerDef;
            }
            _headerDef = Name + Parameters.GetParamDefText("(", ", ", ") return ") + ReturnType.GetDefName();
            return _headerDef;
		}

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public StoredFunction(Db2SourceContext context, string owner, string schema, string objectName, string definition, bool isLoaded) : base(context, owner, schema, objectName, definition, isLoaded)
		{
        }

		public override NamespaceIndex GetCollectionIndex()
		{
			return NamespaceIndex.Objects;
		}
	}
}
