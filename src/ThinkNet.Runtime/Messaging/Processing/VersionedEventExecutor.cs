using System;
using System.Collections;
using System.Linq;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class VersionedEventExecutor : MessageExecutor<VersionedEvent>
    {
        private readonly IHandlerProvider _handlerProvider;
        private readonly IEventBus _eventBus;
        private readonly IEventPublishedVersionStore _eventPublishedVersionStore;
        private readonly IEnvelopeSender _sender;


        public VersionedEventExecutor(IHandlerProvider handlerProvider,
            IEventBus eventBus,
            IEnvelopeSender sender,
            IEventPublishedVersionStore eventPublishedVersionStore)
        {
            this._handlerProvider = handlerProvider;
            this._sender = sender;
            this._eventBus = eventBus;
            this._eventPublishedVersionStore = eventPublishedVersionStore;
        }

        private Envelope Transform(VersionedEvent @event, Exception ex)
        {
            var reply = @event.Events.IsEmpty() ? new RepliedCommand(@event.CommandId) :
                new RepliedCommand(@event.CommandId, ex, CommandReturnType.DomainEventHandled);

            var envelope = new Envelope(reply);
            envelope.Metadata[StandardMetadata.IdentifierId] = reply.Id;
            envelope.Metadata[StandardMetadata.SourceId] = @event.CommandId;
            envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.RepliedCommandKind;

            return envelope;
        }

        protected override void OnExecuted(VersionedEvent @event, ExecutionStatus status)
        {
            switch(status) {
                case ExecutionStatus.Completed:
                    _sender.SendAsync(Transform(@event, null));
                    _eventBus.Publish(@event.Events);
                    break;
                case ExecutionStatus.Awaited:
                    _eventBus.Publish(@event);
                    break;
            }
            base.OnExecuted(@event, status);
        }

        protected override void OnException(VersionedEvent @event, Exception ex)
        {
            _sender.SendAsync(Transform(@event, ex));
            base.OnException(@event, ex);
        }

        protected override ExecutionStatus Execute(VersionedEvent @event)
        {
            var sourceKey = new DataKey(@event.SourceId, @event.SourceType);

            var version = _eventPublishedVersionStore.GetPublishedVersion(sourceKey);
            if(@event.Version > version + 1) { //如果该消息的版本大于要处理的版本则重新进队列等待下次处理
                return ExecutionStatus.Awaited;
            }
            if(@event.Version <= version) { //如果该消息的版本等于要处理的版本则表示已经处理过
                return ExecutionStatus.Obsoleted;
            }

            var eventTypes = @event.Events.Select(p => p.GetType()).ToArray();
            var handler = _handlerProvider.GetEventHandler(eventTypes);
            handler.Handle(this.GetParameter(@event));

            _eventPublishedVersionStore.AddOrUpdatePublishedVersion(sourceKey, @event.Version);

            return ExecutionStatus.Completed;
        }

        public object[] GetParameter(VersionedEvent @event)
        {
            var array = new ArrayList();
            array.Add(@event.Version);

            var collection = @event.Events as ICollection;
            if (collection != null) {
                array.AddRange(collection);
            }
            else {
                foreach (var el in @event.Events)
                    array.Add(el);
            }

            return array.ToArray();
        }
    }
}
