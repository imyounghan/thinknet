using System.Collections.Generic;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 消息处理器的代理
    /// </summary>
    public interface IProxyHandler
    {
        /// <summary>
        /// 处理消息。
        /// </summary>
        void Handle(IMessage message);

        /// <summary>
        /// Get the inner handler.
        /// </summary>
        IHandler GetInnerHandler();
    }
}
