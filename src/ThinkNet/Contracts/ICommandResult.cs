
namespace ThinkNet.Contracts
{
    using System.Collections;
    using System.Runtime.Serialization;

    /// <summary>
    /// 命令处理结果
    /// </summary>
    public interface ICommandResult
    {
        /// <summary>
        /// 状态
        /// </summary>
        ExecutionStatus Status { get; }

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
