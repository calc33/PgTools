using System;

namespace Db2Source
{
    public class QueryResultEventArgs : EventArgs
    {
        public bool IsFailed { get; set; } = false;
        public QueryResultEventArgs() { }
    }
}
