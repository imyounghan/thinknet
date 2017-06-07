using System.Runtime.Serialization;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 返回状态枚举定义
    /// </summary>
    [DataContract]
    public enum ExecutionStatus : byte
    {
        /// <summary>
        /// 错误
        /// </summary>
        [EnumMember]
        Failed = 0,
        /// <summary>
        /// 成功
        /// </summary>
        [EnumMember]
        Success = 1,
        /// <summary>
        /// 没有变化或数据
        /// </summary>
        [EnumMember]
        Nothing = 2,
        /// <summary>
        /// 超时
        /// </summary>
        [EnumMember]
        Timeout = 3,
    }
}
