using System;
using System.Collections.Generic;
using System.Threading;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;


namespace ThinkNet.Kernel
{
    public class EventStreamHandler : 
        IMessageHandler<VersionedEventStream>,
        IMessageHandler<DomainEventStream>
    {
        private readonly IMessageExecutor _eventExecutor;
        private readonly IEventPublishedVersionStore _eventPublishedVersionStore;

        public EventStreamHandler(IMessageExecutor eventExecutor,
            IEventPublishedVersionStore eventPublishedVersionStore)
        {
            this._eventExecutor = eventExecutor;
            this._eventPublishedVersionStore = eventPublishedVersionStore;
        }

        public void Handle(VersionedEventStream @event)
        {
            this.Handle(@event as DomainEventStream);

            _eventPublishedVersionStore.AddOrUpdatePublishedVersion(
                @event.AggregateRoot.SourceId,
                @event.AggregateRoot.SourceTypeName, 
                @event.StartVersion,
                @event.EndVersion);
        }

        public void Handle(DomainEventStream @event)
        {
            if (@event.Events.IsEmpty())
                return;

            @event.Events.ForEach(ExecuteEvent);
        }

        private void ExecuteEvent(IEvent @event)
        {
            int count = 0;
            int retryTimes = 1;

            while (count++ < retryTimes) {
                try {
                    _eventExecutor.Execute(@event);
                    break;
                }
                catch (Exception) {
                    if (count == retryTimes)
                        throw;
                    else
                        Thread.Sleep(1000);
                }
            }
        }
    }
}
