using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common.Composition;
using ThinkNet.Common.Interception.Pipeline;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Proxies;

namespace ThinkNet.Runtime.Executing
{
    public class EventExecutor : Executor
    {
        private readonly IMessageHandlerRecordStore _handlerStore;
        private readonly InterceptorPipeline _pipeline;

        public EventExecutor(IMessageHandlerRecordStore handlerStore)
        {
            this._handlerStore = handlerStore;
            //this._firstInterceptor = new MessageHandlerRecordInterceptor(handlerStore);
            this._pipeline = new InterceptorPipeline(new[] { new MessageHandledInterceptor(handlerStore) });
        }


        protected override IEnumerable<IProxyHandler> GetProxyHandlers(Type type)
        {
            var contractType = typeof(IMessageHandler<>).MakeGenericType(type);
            return ObjectContainer.Instance.ResolveAll(contractType).Cast<IHandler>()
                    .Select(handler => {
                        var method = base.GetCachedHandleMethodInfo(contractType, () => handler.GetType());
                        return new MessageHandlerProxy(handler, method, _pipeline);
                    })
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
