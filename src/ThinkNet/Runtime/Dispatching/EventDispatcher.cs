using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common.Interception.Pipeline;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Agent;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 事件处理程序的代理类
    /// </summary>
    public class EventDispatcher : MessageDispatcher
    {
        private readonly InterceptorPipeline _pipeline;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventDispatcher(IHandlerRecordStore handlerStore)
        {
            this._pipeline = new InterceptorPipeline(new[] { new FilterHandledMessageInterceptor(handlerStore) });
        }



        private IHandlerAgent BuildMessageHandler(object handler, Type contractType)
        {
            var method = HandlerMethodProvider.Instance.GetCachedMethodInfo(contractType, () => handler.GetType());
            return new MessageHandlerAgent(handler, method, _pipeline);
        }

        /// <summary>
        /// 构造事件处理程序
        /// </summary>
        protected override IEnumerable<IHandlerAgent> BuildHandlerAgents(Type type)
        {
            Type contractType;
            var handlers = HandlerFetchedProvider.Instance.GetMessageHandlers(type, out contractType);
            return handlers.Select(handler => BuildMessageHandler(handler, contractType));
        }
    }
}
