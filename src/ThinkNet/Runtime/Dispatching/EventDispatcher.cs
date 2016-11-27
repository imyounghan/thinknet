using System;
using System.Collections.Generic;
using System.Linq;
using ThinkLib;
using ThinkLib.Composition;
using ThinkLib.Interception;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Agent;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 事件处理程序的代理类
    /// </summary>
    public class EventDispatcher : Dispatcher
    {
        private readonly IEnumerable<IInterceptor> _firstInterceptors;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventDispatcher(IObjectContainer container, IMessageHandlerRecordStore handlerStore)
            : base(container)
        {
            this._firstInterceptors = new IInterceptor[] { new FilterHandledMessageInterceptor(handlerStore) };
        }

        protected override IEnumerable<IHandlerAgent> BuildHandlerAgents(Type eventType)
        {
            var contractType = typeof(IMessageHandler<>).MakeGenericType(eventType);

            var handlers = this.GetMessageHandlers(contractType);
            if(handlers.IsEmpty())
                return Enumerable.Empty<IHandlerAgent>();

            var handlerAgentType = typeof(MessageHandlerAgent<>).MakeGenericType(eventType);
            var constructor = handlerAgentType.GetConstructor(new Type[] { contractType, typeof(IEnumerable<IInterceptor>), typeof(IEnumerable<IInterceptor>) });

            return handlers.Select(handler => constructor.Invoke(new object[] { handler, _firstInterceptors, new IInterceptor[0] })).Cast<IHandlerAgent>();
        }

        //private IHandlerAgent BuildMessageHandler(object handler, Type contractType)
        //{
        //    var method = MessageHandlerProvider.Instance.GetCachedHandleMethodInfo(contractType, () => handler.GetType());
        //    return new MessageHandlerAgent(handler, method, _pipeline);
        //}

        ///// <summary>
        ///// 构造事件处理程序
        ///// </summary>
        //protected override IEnumerable<IHandlerAgent> BuildHandlerAgents(Type type)
        //{
        //    Type contractType;
        //    var handlers = MessageHandlerProvider.Instance.GetMessageHandlers(type, out contractType);
        //    return handlers.Select(handler => BuildMessageHandler(handler, contractType));
        //}
    }
}
