using System;
using ThinkNet.Configurations;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 存储处理程序信息的接口
    /// </summary>
    [UnderlyingComponent(typeof(HandlerRecordInMemory))]
    public interface IHandlerRecordStore
    {
        /// <summary>
        /// 添加处理程序信息
        /// </summary>
        void AddHandlerInfo(string messageId, Type messageType, Type handlerType);
        /// <summary>
        /// 返回一个值表示当前的处理程序是否被执行。
        /// </summary>
        bool HandlerIsExecuted(string messageId, Type messageType, Type handlerType);
        /// <summary>
        /// 返回一个值表示当前的处理程序是否存在被执行过的。
        /// </summary>
        bool HandlerHasExecuted(string messageId, Type messageType, params Type[] handlerTypes);
    }
}
