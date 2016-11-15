using System;
using System.Collections.Generic;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Runtime.Executing
{
    public class EventExecutor : Executor<IEvent>
    {
        private readonly IMessageHandlerRecordStore _handlerStore;

        public EventExecutor(IMessageHandlerRecordStore handlerStore)
        {
            this._handlerStore = handlerStore;
        }

        protected override void OnExecuting(IEvent @event, Type handlerType)
        {
            var eventType = @event.GetType();
            if (_handlerStore.HandlerIsExecuted(@event.Id, eventType, handlerType)) {
                var errorMessage = string.Format("The event has been handled. EventHandlerType:{0}, EventType:{1}, EventId:{2}.",
                    handlerType.FullName, eventType.FullName, @event.Id);
                throw new MessageHandlerProcessedException(errorMessage);
            }
        }

        protected override void OnExecuted(IEvent @event, Type handlerType, Exception ex)
        {
            if (ex != null)
                _handlerStore.AddHandlerInfo(@event.Id, @event.GetType(), handlerType);
        }
    }
}
