using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class EventExecutor : MessageExecutor<IEvent>
    {
        private readonly IHandlerProvider _handlerProvider;
        private readonly IHandlerRecordStore _handlerStore;

        public EventExecutor(IHandlerRecordStore handlerStore,
            IHandlerProvider handlerProvider)
        {
            this._handlerProvider = handlerProvider;
            this._handlerStore = handlerStore;
        }

        protected override void Execute(IEvent @event)
        {
            var eventType = @event.GetType();

            foreach(var handler in _handlerProvider.GetMessageHandlers(eventType)) {
                if(_handlerStore.HandlerIsExecuted(@event.Id, eventType, handler.HanderType)) {
                    if(LogManager.Default.IsDebugEnabled)
                        LogManager.Default.DebugFormat("The event has been handled. eventHandlerType:{0}, eventData:{1}.",
                             handler.HanderType.FullName, @event);
                    continue;
                }
                handler.Handle(@event);

                _handlerStore.AddHandlerInfo(@event.Id, eventType, handler.HanderType);
            }
        }
    }
}
