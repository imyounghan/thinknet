using System;
using ThinkNet.Common;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示一个当找不到消息处理程序的异常
    /// </summary>
    [Serializable]
    public class MessageHandlerNotFoundException : ThinkNetException
    {
        /// <summary>Parameterized constructor.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        public MessageHandlerNotFoundException(Type messageType)
            : base(string.Format("Message Handler not found for {0}.", messageType.FullName))
        { }
    }
}
