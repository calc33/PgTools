using System;

namespace Db2Source
{
    public class RowChangedEventArgs : EventArgs
    {
        public Row Row { get; private set; }
        internal RowChangedEventArgs(Row row)
        {
            Row = row;
        }
    }
}
