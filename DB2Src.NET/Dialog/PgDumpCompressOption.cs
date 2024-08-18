using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
	public class PgDumpCompressOption
	{
		public string Value { get; set; }
		public string Text { get; set; }

		public override bool Equals(object obj)
		{
			if (!(obj is PgDumpCompressOption))
			{
				return false;
			}
			return Value.Equals(((PgDumpCompressOption)obj).Value);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public override string ToString()
		{
			return Text;
		}
	}
}
