

namespace ThinkNet.Messaging
{
    using System.Collections;
    using System.Runtime.Serialization;

    /// <summary>
    /// 表示一个用于发布订阅的异常
    /// </summary>
    public interface IPublishableException : ISerializable, IMessage
    {
        //string ErrorCode { get; }

        /// <summary>
        /// 异常消息
        /// </summary>
        string Message { get; }

        /// <summary>
        /// 获取提供有关异常的用户定义信息的键/值对集合。
        /// </summary>
        IDictionary Data { get; }
    }
}
