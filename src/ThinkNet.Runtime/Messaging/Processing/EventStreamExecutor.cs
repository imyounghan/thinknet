using System;
using System.Collections;
using System.Linq;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class EventStreamExecutor : MessageExecutor<EventStream>
    {
        //class ParsedEvent
        //{
        //    public DataKey Key { get; set; }

        //    public string CommandId { get; set; }

        //    public int Version { get; set; }

        //    public IEnumerable<IEvent> Events { get; set; }

        //    public object[] GetParameter()
        //    {
        //        var array = new ArrayList();
        //        array.Add(this.Version);

        //        var collection = this.Events as ICollection;
        //        if(collection != null) {
        //            array.AddRange(collection);
        //        }
        //        else {
        //            foreach(var @event in this.Events)
        //                array.Add(@event);
        //        }

        //        return array.ToArray();
        //    }
        //}


        private readonly IHandlerProvider _handlerProvider;
        private readonly IEventBus _eventBus;
        private readonly IEventPublishedVersionStore _eventPublishedVersionStore;
        private readonly IEnvelopeSender _sender;


        public EventStreamExecutor(IHandlerProvider handlerProvider,
            IEventBus eventBus,
            IEnvelopeSender sender,
            IEventPublishedVersionStore eventPublishedVersionStore)
        {
            this._handlerProvider = handlerProvider;
            this._sender = sender;
            this._eventBus = eventBus;
            this._eventPublishedVersionStore = eventPublishedVersionStore;
        }

        private Envelope Transform(EventStream @event, Exception ex)
        {
            var reply = @event.Events.IsEmpty() ? new CommandReply(@event.CommandId) :
                new CommandReply(@event.CommandId, ex, CommandReturnType.DomainEventHandled);

            var envelope = new Envelope(reply);
            envelope.Metadata[StandardMetadata.CorrelationId] = reply.Id;
            envelope.Metadata[StandardMetadata.RoutingKey] = @event.CommandId;
            envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandReplyKind;

            return envelope;
        }

        protected override void OnExecuted(EventStream @event, ExecutionStatus status)
        {
            switch(status) {
                case ExecutionStatus.Completed:
                    _sender.SendAsync(Transform(@event, null));
                    break;
                case ExecutionStatus.Awaited:
                    _eventBus.Publish(@event);
                    break;
            }
            base.OnExecuted(@event, status);
        }

        protected override void OnException(EventStream @event, Exception ex)
        {
            _sender.SendAsync(Transform(@event, ex));
            base.OnException(@event, ex);
        }

        protected override ExecutionStatus Execute(EventStream @event)
        {
            //if(stream.Events.IsEmpty()) {
            //    _notification.NotifyUnchanged(stream.CommandId);
            //    return;
            //}

            //var @event = new ParsedEvent() {
            //    Key = new DataKey(stream.SourceId, stream.SourceNamespace, stream.SourceTypeName, stream.SourceAssemblyName),
            //    CommandId = stream.CommandId,
            //    Version = stream.Version,
            //    Events = stream.Events.Select(Deserialize).AsEnumerable()
            //};

            //this.Execute(@event);
            var result = this.Synchronize(@event);
            if(result == ExecutionStatus.Completed) {
                _eventBus.Publish(@event.Events);
            }

            return result;
        }

        //private void Execute(ParsedEvent @event)
        //{
        //    var result = this.Synchronize(@event);

        //    switch(result) {
        //        case SynchronizeStatus.Complete:
        //            _notification.NotifyCompleted(@event.CommandId);
        //            _eventBus.Publish(@event.Events);
        //            break;
        //        case SynchronizeStatus.Retry:
        //            _retryQueue.TryAdd(@event, 5000);
        //            break;
        //    }
        //}

        private ExecutionStatus Synchronize(EventStream @event)
        {
            var key = new DataKey(@event.SourceId, @event.SourceNamespace, @event.SourceTypeName, @event.SourceAssemblyName);

            var version = _eventPublishedVersionStore.GetPublishedVersion(key);
            if(@event.Version > version + 1) { //如果该消息的版本大于要处理的版本则重新进队列等待下次处理
                return ExecutionStatus.Awaited;
            }
            if(@event.Version <= version) { //如果该消息的版本等于要处理的版本则表示已经处理过
                return ExecutionStatus.Obsoleted;
            }

            var eventTypes = @event.Events.Select(p => p.GetType()).ToArray();
            var handler = _handlerProvider.GetEventHandler(eventTypes);
            handler.Handle(this.GetParameter(@event));

            _eventPublishedVersionStore.AddOrUpdatePublishedVersion(key, @event.Version);

            return ExecutionStatus.Completed;
        }

        public object[] GetParameter(EventStream @event)
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
        //private IEvent Deserialize(EventStream.Stream stream)
        //{
        //    return (IEvent)_serializer.Deserialize(stream.Payload, stream.GetSourceType());
        //}
    }
}
