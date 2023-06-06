using System;
using System.Collections;
using System.Text;

namespace Db2Source
{
    public class ArrayFormatProvider : IFormatProvider, ICustomFormatter
    {
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg == null)
            {
                return null;
            }
            StringBuilder buf = new StringBuilder();
            buf.Append('(');
            bool needComma = false;
            foreach (object o in (IEnumerable)arg)
            {
                if (needComma)
                {
                    buf.Append(", ");
                }
                buf.Append(o);
                needComma = true;
            }
            buf.Append(')');
            return buf.ToString();
        }

        public object GetFormat(Type formatType)
        {
            if (formatType.IsArray || formatType.IsSubclassOf(typeof(IEnumerable)))
            {
                return this;
            }
            return null;
        }
    }
}
