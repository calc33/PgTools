namespace Db2Source
{
    public enum AxisEntryStatus
    {
        /// <summary>
        /// 不明・未設定
        /// </summary>
        Unkonwn = -1,
        /// <summary>
        /// 非表示
        /// </summary>
        Hidden,
        /// <summary>
        /// この値を表示
        /// </summary>
        Visible,
        /// <summary>
        /// 同一エントリーの直前と結合
        /// </summary>
        JoinPriorLevel,
        /// <summary>
        /// 一つ前のエントリーと結合
        /// </summary>
        JoinPriorEntry,
        ///// <summary>
        ///// 直前・直上のエントリー両方と結合
        ///// </summary>
        //JoinPriorEntryAndLevel
    }
}
