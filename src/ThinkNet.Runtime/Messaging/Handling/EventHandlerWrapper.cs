
namespace ThinkNet.Messaging.Handling
{
    public class EventHandlerWrapper<TEvent> : MessageHandlerWrapper<TEvent>
        where TEvent : class, IVersionedEvent
    {
        private readonly IEventContextFactory _eventContextFactory;

        public EventHandlerWrapper(IHandler handler, IEventContextFactory eventContextFactory)
            : base(handler)
        {
            this._eventContextFactory = eventContextFactory;
        }

        public override void Handle(TEvent @event)
        {
            var eventHandler = this.GetInnerHandler() as IEventHandler<TEvent>;
            if (eventHandler == null)
                return;

            var context = _eventContextFactory.GetCurrentEventContext();
            eventHandler.Handle(context, @event);
        }
    }
}
