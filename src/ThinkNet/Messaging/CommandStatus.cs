
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 命令处理状态枚举定义
    /// </summary>
    [System.Serializable]
    public enum CommandStatus
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
        /// 没有变化
        /// </summary>
        NothingChanged = 2,
        /// <summary>
        /// 超时
        /// </summary>
        Timeout = 3,
    }
}
