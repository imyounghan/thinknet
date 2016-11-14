using System;
using System.Collections.Generic;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Runtime.Executing
{
    public class EventExecutor : Executor<IEvent>
    {
        private readonly IHandlerRecordStore _handlerStore;

        public EventExecutor(IHandlerRecordStore handlerStore)
        {
            this._handlerStore = handlerStore;
        }

        protected override void OnExecuting(IEvent @event, Type handlerType)
        {
            var eventType = @event.GetType();
            if (_handlerStore.HandlerIsExecuted(@event.Id, eventType, handlerType)) {
                var errorMessage = string.Format("The event has been handled. eventHandlerType:{0}, eventType:{0}, eventId:{1}.",
                    handlerType.FullName, eventType.FullName, @event.Id);                
                throw new MessageHandlerProcessedException();
            }
        }

        protected override void OnExecuted(IEvent @event, Type handlerType, Exception ex)
        {
            if (ex != null)
                _handlerStore.AddHandlerInfo(@event.Id, @event.GetType(), handlerType);
        }
    }
}
