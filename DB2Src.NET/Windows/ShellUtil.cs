using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source.Windows
{
	public static class ShellUtil
	{
		private static readonly Dictionary<char, bool> QuoteChars = new Dictionary<char, bool>()
		{
			{ ' ', true },
			{ '<', true },
			{ '>', true },
			{ '|', true },
			{ '(', true },
			{ ')', true },
			{ '&', true },
		};
		public static string TryQuote(string value)
		{
			StringBuilder buf = new StringBuilder();
			bool needQuote = false;
			bool wasBackslash = false;
			foreach (char c in value)
			{
				if (QuoteChars.ContainsKey(c))
				{
					needQuote = true;
				}
				switch (c)
				{
					case '"':
						if (wasBackslash)
						{
							buf.Append('\\');
						}
						buf.Append('\\');
						break;
					case '%':
						buf.Append('^');
						break;
				}
				buf.Append(c);
				wasBackslash = (c == '\\');
			}
            if (needQuote)
            {
				buf.Insert(0, '"');
				buf.Append('"');
            }
            return buf.ToString();
		}
	}
}
