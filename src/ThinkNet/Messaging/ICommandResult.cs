

namespace ThinkNet.Messaging
{
    using System.Collections;

    public interface ICommandResult : IReplyResult
    {
        /// <summary>
        /// 错误编码
        /// </summary>
        string ErrorCode { get; }

        /// <summary>
        /// 设置或获取一个提供用户定义的其他异常信息的键/值对的集合。
        /// </summary>
        IDictionary ErrorData { get; }
    }
}
