using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common.Composition;
using ThinkNet.Common.Interception.Pipeline;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Proxies;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 事件处理程序的代理类
    /// </summary>
    public class EventDispatcher : MessageDispatcher
    {
        private readonly InterceptorPipeline _pipeline;
        private readonly IHandlerMethodProvider _handlerMethodProvider;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventDispatcher(IHandlerRecordStore handlerStore,
            IHandlerMethodProvider handlerMethodProvider)
        {
            this._handlerMethodProvider = handlerMethodProvider;
            this._pipeline = new InterceptorPipeline(new[] { new FilterHandledMessageInterceptor(handlerStore) });
        }



        private IHandlerProxy BuildMessageHandler(object handler, Type contractType)
        {
            var method = _handlerMethodProvider.GetCachedMethodInfo(handler.GetType(), contractType);
            return new MessageHandlerProxy(handler, method, _pipeline);
        }

        /// <summary>
        /// 获取事件类型的Handler
        /// </summary>
        protected override IEnumerable<IHandlerProxy> GetProxyHandlers(Type type)
        {
            var contractType = typeof(IMessageHandler<>).MakeGenericType(type);
            return ObjectContainer.Instance.ResolveAll(contractType)
                    .Select(handler => BuildMessageHandler(handler, contractType))
                    .ToArray();
        }

        //protected override void OnExecuting(IEvent @event, Type handlerType)
        //{
        //    var eventType = @event.GetType();
        //    if (_handlerStore.HandlerIsExecuted(@event.Id, eventType, handlerType)) {
        //        var errorMessage = string.Format("The event has been handled. EventHandlerType:{0}, EventType:{1}, EventId:{2}.",
        //            handlerType.FullName, eventType.FullName, @event.Id);
        //        throw new MessageHandlerProcessedException(errorMessage);
        //    }
        //}

        //protected override void OnExecuted(IEvent @event, Type handlerType, Exception ex)
        //{
        //    if (ex != null)
        //        _handlerStore.AddHandlerInfo(@event.Id, @event.GetType(), handlerType);
        //}
    }
}
