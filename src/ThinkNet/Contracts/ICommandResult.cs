using System.Collections;
using System.Runtime.Serialization;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 命令处理结果
    /// </summary>
    public interface ICommandResult : IDataTransferObject
    {
        /// <summary>
        /// 命令处理状态。
        /// </summary>
        [DataMember]
        CommandStatus Status { get; }
        ///// <summary>
        ///// Represents the unique identifier of the command.
        ///// </summary>
        //[DataMember]
        //string CommandId { get; }
        /// <summary>
        /// 错误消息
        /// </summary>
        [DataMember]
        string ErrorMessage { get; }
        /// <summary>
        /// 错误编码
        /// </summary>
        [DataMember]
        string ErrorCode { get; }
        /// <summary>
        /// 设置或获取一个提供用户定义的其他异常信息的键/值对的集合。
        /// </summary>
        [DataMember]
        IDictionary ErrorData { get; }
    }
}
