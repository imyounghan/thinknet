using System;
using ThinkNet.Common;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示一个存在多个消息处理程序的异常
    /// </summary>
    public class MessageHandlerTooManyException : ThinkNetException
    {
        /// <summary>Parameterized constructor.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        public MessageHandlerTooManyException(Type messageType)
            : base(string.Format("Found more than one message handler, messageType:{0}.", messageType.FullName)) 
        { }
    }
}
