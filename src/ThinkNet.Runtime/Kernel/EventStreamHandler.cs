using System;
using System.Collections.Generic;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;


namespace ThinkNet.Kernel
{
    public class EventStreamHandler : 
        IMessageHandler<VersionedEventStream>,
        IMessageHandler<EventStream>
    {
        private readonly IMessageExecutor _executor;
        private readonly IEventPublishedVersionStore _eventPublishedVersionStore;

        public EventStreamHandler(IMessageExecutor executor,
            IEventPublishedVersionStore eventPublishedVersionStore)
        {
            this._executor = executor;
            this._eventPublishedVersionStore = eventPublishedVersionStore;
        }

        public void Handle(VersionedEventStream @event)
        {
            try {
                this.Handle(@event as EventStream);
            }
            catch (Exception) {
                throw;
            }
            finally {
                _eventPublishedVersionStore.AddOrUpdatePublishedVersion(
                    new SourceKey(@event.SourceId, @event.SourceNamespace, @event.SourceTypeName, @event.SourceAssemblyName),
                    @event.StartVersion,
                    @event.EndVersion);
            }
        }

        public void Handle(EventStream @event)
        {
            if (@event.Events.IsEmpty())
                return;

            @event.Events.ForEach(_executor.Execute);
        }
    }
}
