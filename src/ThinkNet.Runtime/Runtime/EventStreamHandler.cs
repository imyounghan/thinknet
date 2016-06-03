using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThinkLib.Contexts;
using ThinkLib.Scheduling;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;


namespace ThinkNet.Runtime
{
    internal class EventStreamHandler : IHandler<EventStream>
    {
        enum SynchronizeStatus
        {
            Pass,
            Complete,
            Processed,
            Retry,
            Obsolete
        }

        class ParsedEvent
        {
            public SourceKey Key { get; set; }

            public string CommandId { get; set; }

            public int StartVersion { get; set; }

            public int EndVersion { get; set; }

            public IEnumerable<IVersionedEvent> Events { get; set; }
        }

        private readonly IEventContextFactory _eventContextFactory; 
        private readonly IEventPublishedVersionStore _eventPublishedVersionStore;
        private readonly ITextSerializer _serializer;
        private readonly IMessageNotification _notification;
        private readonly IHandlerProvider _handlerProvider;

        private readonly BlockingCollection<ParsedEvent> retryQueue;
        private readonly BlockingCollection<IVersionedEvent> pendingQueue;
        private readonly Worker[] workers;

        public EventStreamHandler(IEventPublishedVersionStore eventPublishedVersionStore,
            ITextSerializer serializer,
            IEventContextFactory eventContextFactory,
            IMessageNotification notification,
            IHandlerProvider handlerProvider)
        {
            this._eventPublishedVersionStore = eventPublishedVersionStore;
            this._serializer = serializer;
            this._eventContextFactory = eventContextFactory;
            this._notification = notification;
            this._handlerProvider = handlerProvider;

            this.workers = new Worker[2];
            this.retryQueue = new BlockingCollection<ParsedEvent>();
            workers[0] = WorkerFactory.Create(Retry);
            this.pendingQueue = new BlockingCollection<IVersionedEvent>();
            workers[1] = WorkerFactory.Create(Dispatch);

            workers.ForEach(worker => worker.Start());
        }

        private void Retry()
        {
            var @event = retryQueue.Take(workers[0].CancellationToken);
            this.Execute(@event);
        }

        private void Dispatch()
        {
            var @event = pendingQueue.Take(workers[0].CancellationToken);
            MessageCenter<IEvent>.Instance.TryAdd(new Message<IEvent> {
                Body = @event,
                RoutingKey = @event.SourceId
            });
        }

        public void Handle(EventStream stream)
        {
            if (stream.Events.IsEmpty()) {
                _notification.NotifyMessageUntreated(stream.CommandId);
                return;
            }

            var @event = new ParsedEvent() {
                Key = new SourceKey(stream.SourceId, stream.SourceNamespace, stream.SourceTypeName, stream.SourceAssemblyName),
                CommandId = stream.CommandId,
                StartVersion = stream.StartVersion,
                EndVersion = stream.EndVersion,
                Events = stream.Events.Select(Deserialize).Cast<IVersionedEvent>().AsEnumerable()
            };

            this.Execute(@event);
        }

        private void Execute(ParsedEvent @event)
        {
            var status = this.Synchronize(@event);

            if (status == SynchronizeStatus.Pass || status == SynchronizeStatus.Complete) {
                foreach (var item in @event.Events) {                    
                    pendingQueue.TryAdd(item, 5000, workers[1].CancellationToken);
                }
            }

            if (status == SynchronizeStatus.Complete) {
                _notification.NotifyMessageCompleted(@event.CommandId);
            }
        }

        private SynchronizeStatus Synchronize(ParsedEvent @event)
        {
            var dict = @event.Events.ToDictionary(p => p.GetType(), p => _handlerProvider.GetEventHandler(p.GetType()));

            if (dict.Values.All(item => item.IsNull()))
                return SynchronizeStatus.Pass;

            var version = _eventPublishedVersionStore.GetPublishedVersion(@event.Key);
            if (@event.StartVersion > version + 1) { //如果该消息的版本大于要处理的版本则重新进队列等待下次处理
                retryQueue.TryAdd(@event, 5000, workers[0].CancellationToken);
                return SynchronizeStatus.Retry;
            }
            if (@event.EndVersion == version) { //如果该消息的版本等于要处理的版本则表示已经处理过
                return SynchronizeStatus.Processed;
            }
            if (@event.StartVersion < version) { //如果该消息的版本小于要处理的版本则表示已经过时
                return SynchronizeStatus.Obsolete;
            }

            foreach (var kv in dict) {
                if (kv.Value.IsNull()) {
                    throw new MessageHandlerNotFoundException(kv.Key);
                }
            }

            using (var context = _eventContextFactory.CreateEventContext()) {
                CurrentContext.Bind(context);

                foreach (var item in @event.Events) {
                    dict[item.GetType()].Handle(item);
                }
                context.Commit();

                CurrentContext.Unbind(context.ContextManager);
            }

            _eventPublishedVersionStore.AddOrUpdatePublishedVersion(@event.Key, @event.StartVersion, @event.EndVersion);

            return SynchronizeStatus.Complete;
        }
        
        private object Deserialize(EventStream.Stream stream)
        {
            return _serializer.Deserialize(stream.Payload, stream.GetSourceType());
        }
    }
}
