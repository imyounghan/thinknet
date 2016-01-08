using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;


namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 消息处理程序的提供者
    /// </summary>
    //[RequiredComponent(typeof(DefaultHandlerProvider))]
    public interface IHandlerProvider
    {
        /// <summary>
        /// 获取该消息类型的所有的处理程序。
        /// </summary>
        IEnumerable<IProxyHandler> GetHandlers(Type type);
    }
}
