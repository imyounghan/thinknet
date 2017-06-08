

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 返回状态枚举定义
    /// </summary>
    public enum ExecutionStatus : byte
    {
        /// <summary>
        /// 错误
        /// </summary>
        Failed = 0,
        /// <summary>
        /// 成功
        /// </summary>
        Success = 1,
        /// <summary>
        /// 没有变化或数据
        /// </summary>
        Nothing = 2,
        /// <summary>
        /// 超时
        /// </summary>
        Timeout = 3,
    }
}
