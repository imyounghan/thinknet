using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinkNet.Common;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class EventProcessor : MessageProcessor<IEvent>
    {
        private readonly IHandlerProvider _handlerProvider;
        private readonly IHandlerRecordStore _handlerStore;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventProcessor(IHandlerRecordStore handlerStore,
            IHandlerProvider handlerProvider,
            IEnvelopeDelivery envelopeDelivery)
            : base(envelopeDelivery)
        {
            this._handlerProvider = handlerProvider;
            this._handlerStore = handlerStore;
        }

        protected override void Execute(IEvent @event)
        {
            var eventType = @event.GetType();

            foreach (var handler in _handlerProvider.GetMessageHandlers(eventType)) {
                if (_handlerStore.HandlerIsExecuted(@event.Id, eventType, handler.HanderType)) {
                    if (LogManager.Default.IsDebugEnabled)
                        LogManager.Default.DebugFormat("The event has been handled. eventHandlerType:{0}, eventType:{1}, eventInfo:{2}",
                             handler.HanderType.FullName, eventType.FullName, @event);
                    continue;
                }
                handler.Handle(@event);

                _handlerStore.AddHandlerInfo(@event.Id, eventType, handler.HanderType);
            }
        }
    }
}
