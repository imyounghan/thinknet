
namespace ThinkNet.Contracts
{
    /// <summary>
    /// 查询结果状态枚举定义
    /// </summary>
    [System.Serializable]
    public enum QueryStatus
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
        /// 超时
        /// </summary>
        Timeout = 2,
    }
}
