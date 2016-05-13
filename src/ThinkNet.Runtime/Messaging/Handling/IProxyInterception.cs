using System;

namespace ThinkNet.Messaging.Handling
{
    public interface IProxyInterception
    {
        /// <summary>
        /// 在处理消息之前调用
        /// </summary>
        void OnHandlerExecuting(IMessage message);
        /// <summary>
        /// 在处理消息之后调用
        /// </summary>
        void OnHandlerExecuted(IMessage message, Exception exception);

        /// <summary>
        /// Get the inner interception.
        /// </summary>
        IInterception GetInnerInterception();
    }
}
