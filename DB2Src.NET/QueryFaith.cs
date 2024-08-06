using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db2Source
{
    public enum QueryFaith
    {
        /// <summary>
        /// クエリを実行していない状態(開始前/終了後)
        /// </summary>
        Idle,
        /// <summary>
        /// クエリを実行中(中断不可)
        /// </summary>
        Startup,
        /// <summary>
        /// クエリを実行中(中断可)
        /// </summary>
        Abortable,
    }
}
