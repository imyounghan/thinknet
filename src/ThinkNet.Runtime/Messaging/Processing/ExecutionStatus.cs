namespace ThinkNet.Messaging.Processing
{
    public enum ExecutionStatus
    {
        /// <summary>
        /// 表示处理完成
        /// </summary>
        Completed,
        ///// <summary>
        ///// 失败
        ///// </summary>
        //Failed,
        /// <summary>
        /// 等待处理
        /// </summary>
        Awaited,
        /// <summary>
        /// 表示已处理过的
        /// </summary>
        Obsoleted
    }
}
