using System;
using System.Linq;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Runtime
{
    public class EventProcessor : MessageProcessor<IEvent>
    {
        private readonly IMessageNotification _notification;
        private readonly IHandlerProvider _handlerProvider;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventProcessor(IMessageNotification notification,
            IHandlerRecordStore handlerStore,
            IHandlerProvider handlerProvider)
            : base(handlerStore)
        {
            this._notification = notification;
            this._handlerProvider = handlerProvider;
        }

        protected override void Execute(IEvent @event)
        {
            var eventType = @event.GetType();
            if (@event is EventStream) {
                var handler = _handlerProvider.GetHandlers(eventType).FirstOrDefault();
                DuplicateProcessHandler(handler, @event, eventType);
                return;
            }

            foreach (var handler in _handlerProvider.GetHandlers(eventType)) {
                OnlyonceProcessHandler(handler, @event, eventType);
            }
        }

        protected override void Notify(IEvent @event, Exception exception)
        {
            var stream  = @event as EventStream;
            if (stream != null && exception != null) {
                _notification.NotifyMessageCompleted(stream.CommandId, exception);
            }
        }  
    }
}
