
using System.Runtime.Serialization;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 查询结果状态枚举定义
    /// </summary>
    [DataContract]
    public enum QueryStatus
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
        /// 超时
        /// </summary>
        [EnumMember]
        Timeout = 2,
    }
}
