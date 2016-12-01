using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 命令处理状态枚举定义
    /// </summary>
    [DataContract]
    public enum CommandStatus : byte
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
        /// 没有变化
        /// </summary>
        [EnumMember]
        NothingChanged = 2,
        /// <summary>
        /// 超时
        /// </summary>
        [EnumMember]
        Timeout = 3,
    }
}
