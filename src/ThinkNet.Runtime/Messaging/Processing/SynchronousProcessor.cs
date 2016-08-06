using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    internal class SynchronousProcessor : MessageProcessor<EventStream>
    {
        private readonly ICommandNotification _notification;
        private readonly IHandlerProvider _handlerProvider;
        private readonly IEventBus _eventBus;
        private readonly IEventContextFactory _eventContextFactory;
        private readonly IEventPublishedVersionStore _eventPublishedVersionStore;
        private readonly ISerializer _serializer;

        private readonly BlockingCollection<ParsedEvent> retryQueue;
        private readonly ConcurrentQueue<IEvent> pendingQueue;
        private readonly IList<IEvent> tempList;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public SynchronousProcessor(ICommandNotification notification,
            IHandlerProvider handlerProvider,
            IEventBus eventBus,
            IEventPublishedVersionStore eventPublishedVersionStore,
            ISerializer serializer,
            IEventContextFactory eventContextFactory,
            IEnvelopeDelivery envelopeDelivery)
            : base(envelopeDelivery)
        {
            this._notification = notification;
            this._handlerProvider = handlerProvider;
            this._eventPublishedVersionStore = eventPublishedVersionStore;
            this._serializer = serializer;
            this._eventContextFactory = eventContextFactory;
            this._eventBus = eventBus;

            this.tempList = new List<IEvent>();
            this.retryQueue = new BlockingCollection<ParsedEvent>();
            base.BuildWorker<ParsedEvent>(retryQueue.Take, Execute);
            this.pendingQueue = new ConcurrentQueue<IEvent>();
            base.BuildWorker(Dispatch);
        }

        protected override string GetRoutingKey(EventStream data)
        {
            return data.SourceId;
        }

        protected override void Notify(EventStream @event, Exception exception)
        {
            _notification.NotifyCompleted(@event.CommandId, exception);
        }


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
            public DataKey Key { get; set; }

            public string CommandId { get; set; }

            public int Version { get; set; }

            public IEnumerable<IEvent> Events { get; set; }
        }



        private void Dispatch()
        {
            IEvent @event;
            while (pendingQueue.TryDequeue(out @event)) {
                tempList.Add(@event);
                if (tempList.Count >= 5)
                    break;
            }

            if (tempList.Count == 0)
                System.Threading.Thread.Sleep(1000);

            _eventBus.Publish(tempList);
            tempList.Clear();
        }

        protected override void Execute(EventStream stream)
        {
            if (stream.Events.IsEmpty()) {
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

            switch (result) {
                case SynchronizeStatus.Complete:
                    _notification.NotifyCompleted(@event.CommandId);
                    @event.Events.ForEach(pendingQueue.Enqueue);
                    break;
                case SynchronizeStatus.Retry:
                    retryQueue.TryAdd(@event, 5000);
                    break;
            }
        }

        private SynchronizeStatus Synchronize(ParsedEvent @event)
        {
            var version = _eventPublishedVersionStore.GetPublishedVersion(@event.Key);
            if (@event.Version > version + 1) { //如果该消息的版本大于要处理的版本则重新进队列等待下次处理
                retryQueue.TryAdd(@event, 5000);
                return SynchronizeStatus.Retry;
            }
            if (@event.Version == version) { //如果该消息的版本等于要处理的版本则表示已经处理过
                return SynchronizeStatus.Processed;
            }
            if (@event.Version < version) { //如果该消息的版本小于要处理的版本则表示已经过时
                return SynchronizeStatus.Obsolete;
            }

            bool success = true;
            _eventContextFactory.Bind();
            try {                
                @event.Events.ForEach(ProcessHandler);
            }
            catch (Exception) {
                success = false;
                throw;
            }
            finally {
                _eventContextFactory.Unbind(success);
            }

            _eventPublishedVersionStore.AddOrUpdatePublishedVersion(@event.Key, @event.Version);

            return SynchronizeStatus.Complete;
        }

        void ProcessHandler(IEvent @event)
        {
            var eventType = @event.GetType();
            var handler = _handlerProvider.GetEventHandler(eventType);
            if (handler == null)
                throw new MessageHandlerNotFoundException(eventType);
            handler.Handle(@event);
        }

        private IEvent Deserialize(EventStream.Stream stream)
        {
            return (IEvent)_serializer.Deserialize(stream.Payload, stream.GetSourceType());
        }
    }
}
