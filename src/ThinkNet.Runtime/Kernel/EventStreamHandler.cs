using System;
using System.Collections.Generic;
using System.Threading;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
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
            this.Handle(@event as EventStream);

            _eventPublishedVersionStore.AddOrUpdatePublishedVersion(
                new SourceKey(@event.SourceId, @event.SourceNamespace, @event.SourceTypeName, @event.SourceAssemblyName), 
                @event.StartVersion,
                @event.EndVersion);
        }

        public void Handle(EventStream @event)
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
                    _executor.Execute(@event);
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
