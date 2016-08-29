using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class SynchronousExecutor : MessageExecutor<EventStream>
    {
        class ParsedEvent
        {
            public DataKey Key { get; set; }

            public string CommandId { get; set; }

            public int Version { get; set; }

            public IEnumerable<IEvent> Events { get; set; }

            public object[] GetParameter()
            {
                var array = new ArrayList();
                array.Add(this.Version);

                var collection = this.Events as ICollection;
                if(collection != null) {
                    array.AddRange(collection);
                }
                else {
                    foreach(var @event in this.Events)
                        array.Add(@event);
                }

                return array.ToArray();
            }
        }

        enum SynchronizeStatus
        {
            Complete,
            Processed,
            Retry,
            Obsolete
        }

        private readonly ICommandNotification _notification;
        private readonly IHandlerProvider _handlerProvider;

        private readonly IEventBus _eventBus;
        private readonly IEventPublishedVersionStore _eventPublishedVersionStore;
        private readonly ISerializer _serializer;

        private readonly BlockingCollection<ParsedEvent> _retryQueue;


        public SynchronousExecutor(ICommandNotification notification, 
            IHandlerProvider handlerProvider,
            IEventBus eventBus,
            IEventPublishedVersionStore eventPublishedVersionStore,
            ISerializer serializer)
        {
            this._notification = notification;
            this._handlerProvider = handlerProvider;

            this._eventBus = eventBus;
            this._eventPublishedVersionStore = eventPublishedVersionStore;
            this._serializer = serializer;

            this._retryQueue = new BlockingCollection<ParsedEvent>();

            Task.Factory.StartNew(() => {
                while(true) {
                    var @event = _retryQueue.Take();
                    this.Execute(@event);
                }
            }, TaskCreationOptions.LongRunning);
        }

        protected override void Execute(EventStream stream)
        {
            if(stream.Events.IsEmpty()) {
                _notification.NotifyUnchanged(stream.CommandId);
                return;
            }

            var @event = new ParsedEvent() {
                Key = new DataKey(stream.SourceId, stream.SourceNamespace, stream.SourceTypeName, stream.SourceAssemblyName),
                CommandId = stream.CommandId,
                Version = stream.Version,
                Events = stream.Events.Select(Deserialize).AsEnumerable()
            };

            this.Execute(@event);
        }

        private void Execute(ParsedEvent @event)
        {
            var result = this.Synchronize(@event);

            switch(result) {
                case SynchronizeStatus.Complete:
                    _notification.NotifyCompleted(@event.CommandId);
                    _eventBus.Publish(@event.Events);
                    break;
                case SynchronizeStatus.Retry:
                    _retryQueue.TryAdd(@event, 5000);
                    break;
            }
        }

        private SynchronizeStatus Synchronize(ParsedEvent @event)
        {
            var version = _eventPublishedVersionStore.GetPublishedVersion(@event.Key);
            if(@event.Version > version + 1) { //如果该消息的版本大于要处理的版本则重新进队列等待下次处理
                return SynchronizeStatus.Retry;
            }
            if(@event.Version == version) { //如果该消息的版本等于要处理的版本则表示已经处理过
                return SynchronizeStatus.Processed;
            }
            if(@event.Version < version) { //如果该消息的版本小于要处理的版本则表示已经过时
                return SynchronizeStatus.Obsolete;
            }

            var eventTypes = @event.Events.Select(p => p.GetType()).ToArray();
            var handler = _handlerProvider.GetEventHandler(eventTypes);
            handler.Handle(@event.GetParameter());

            _eventPublishedVersionStore.AddOrUpdatePublishedVersion(@event.Key, @event.Version);

            return SynchronizeStatus.Complete;
        }

        private IEvent Deserialize(EventStream.Stream stream)
        {
            return (IEvent)_serializer.Deserialize(stream.Payload, stream.GetSourceType());
        }
    }
}
