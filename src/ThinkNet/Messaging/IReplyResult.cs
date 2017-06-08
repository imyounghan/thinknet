
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 回复处理结果
    /// </summary>
    public interface IReplyResult
    {
        /// <summary>
        /// 状态
        /// </summary>
        ExecutionStatus Status { get; }

        /// <summary>
        /// 错误消息
        /// </summary>
        string ErrorMessage { get; }        
    }
}
