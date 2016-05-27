using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.ServiceLocation;
using ThinkLib.Common;
using ThinkLib.Scheduling;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;


namespace ThinkNet.Runtime
{
    internal class EventStreamHandler : IInitializer, IHandler<EventStream>
    {
        enum SynchronizeStatus
        {
            Pass,
            Complete,
            Retry
        }

        private readonly IEventContextFactory _eventContextFactory; 
        private readonly IEventPublishedVersionStore _eventPublishedVersionStore;
        private readonly ITextSerializer _serializer;
        private readonly IMessageNotification _notification;
        private readonly IMessageBroker _broker;
        private readonly IRoutingKeyProvider _routingKeyProvider;
        private readonly IMetadataProvider _metadataProvider;

        private readonly BlockingCollection<EventStream> queue;
        private readonly Worker worker;

        public EventStreamHandler(IEventPublishedVersionStore eventPublishedVersionStore,
            ITextSerializer serializer,
            IEventContextFactory eventContextFactory,
            IMessageNotification notification)
        {
            this._eventPublishedVersionStore = eventPublishedVersionStore;
            this._serializer = serializer;
            this._eventContextFactory = eventContextFactory;
            this._notification = notification;

            this.queue = new BlockingCollection<EventStream>();
            this.worker = WorkerFactory.Create(Retry);            
        }

        private void Retry()
        {
            var stream = queue.Take(worker.CancellationToken);
            this.Handle(stream);
        }

        public void Handle(EventStream stream)
        {
            if (stream.Events.IsEmpty()) {
                _notification.NotifyMessageUntreated(stream.CommandId);
                return;
            }
            var events = stream.Events.Select(Deserialize).Cast<IVersionedEvent>().AsEnumerable();


            try {
                switch (Synchronize(stream, events)) {
                    case SynchronizeStatus.Complete:
                        _notification.NotifyMessageCompleted(stream.CommandId);
                        break;
                    case SynchronizeStatus.Retry:
                        return;
                }

                //events.Select(SerializeToMessage).ForEach(_broker.Add);                
            }
            catch (Exception ex) {
                _notification.NotifyMessageCompleted(stream.CommandId, ex);
                throw;
            }
        }

        private Message SerializeToMessage(IVersionedEvent @event)
        {
            return new Message {
                Body = @event,
                MetadataInfo = _metadataProvider.GetMetadata(@event),
                RoutingKey = _routingKeyProvider.GetRoutingKey(@event),
                CreatedTime = DateTime.UtcNow
            };
        }

        private SynchronizeStatus Synchronize(EventStream stream, IEnumerable<IVersionedEvent> events)
        {
            var sourceKey = new SourceKey(stream.SourceId, stream.SourceNamespace, stream.SourceTypeName, stream.SourceAssemblyName);
            var version = _eventPublishedVersionStore.GetPublishedVersion(sourceKey);

            if (version + 1 != stream.StartVersion) { //如果当前的消息版本不是要处理的情况
                if (stream.StartVersion > version + 1) //如果该消息的版本大于要处理的版本则重新进队列等待下次处理
                    queue.TryAdd(stream, 5000, worker.CancellationToken);
                return SynchronizeStatus.Retry;
            }

            using (var context = _eventContextFactory.CreateEventContext()) {
                ExecuteAll(context, events);
                context.Commit();
            }            

            _eventPublishedVersionStore.AddOrUpdatePublishedVersion(sourceKey, stream.StartVersion, stream.EndVersion);


            return SynchronizeStatus.Complete;
        }
        
        private object Deserialize(EventStream.Stream stream)
        {
            return _serializer.Deserialize(stream.Payload, stream.GetSourceType());
        }

        private void ExecuteAll(IEventContext context, IEnumerable<IVersionedEvent> events)
        {
            foreach (var @event in events) {
                var eventType = @event.GetType();
                var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
                var handler = ServiceLocator.Current.GetInstance(handlerType);
                if (handler.IsNull())
                    throw new MessageHandlerNotFoundException(eventType);

                ((dynamic)handler).Handle(context, (dynamic)@event);
            }
        }


        #region IInitializer 成员

        public void Initialize(IEnumerable<Type> types)
        {
            worker.Start();
        }

        #endregion
    }
}
