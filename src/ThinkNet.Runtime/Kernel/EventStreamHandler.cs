using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThinkLib.Common;
using ThinkLib.Scheduling;
using ThinkLib.Serialization;
using ThinkNet.EventSourcing;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;


namespace ThinkNet.Kernel
{
    public class EventStreamHandler : IInitializer,
        IMessageHandler<VersionedEventStream>,
        IMessageHandler<EventStream>
    {
        private readonly IMessageExecutor _executor;
        private readonly IEventPublishedVersionStore _eventPublishedVersionStore;
        private readonly ITextSerializer _serializer;

        private readonly BlockingCollection<VersionedEventStream> queue;
        private readonly Worker worker;

        public EventStreamHandler(IMessageExecutor executor,
            IEventPublishedVersionStore eventPublishedVersionStore,
            ITextSerializer serializer)
        {
            this._executor = executor;
            this._eventPublishedVersionStore = eventPublishedVersionStore;
            this._serializer = serializer;

            this.queue = new BlockingCollection<VersionedEventStream>();
            this.worker = WorkerFactory.Create(Retry);            
        }

        private void Retry()
        {
            var stream = queue.Take(worker.CancellationToken);
            this.Handle(stream);
        }

        public void Handle(VersionedEventStream stream)
        {
            var sourceKey = new SourceKey(stream.SourceId, stream.SourceNamespace, stream.SourceTypeName, stream.SourceAssemblyName);
            var version = _eventPublishedVersionStore.GetPublishedVersion(sourceKey);

            if (version + 1 != stream.StartVersion) { //如果当前的消息版本不是要处理的情况
                if (stream.StartVersion > version + 1) //如果该消息的版本大于要处理的版本则重新进队列等待下次处理
                    queue.TryAdd(stream, 5000, worker.CancellationToken);
                return;
            }

            try {
                this.Handle(stream as EventStream);
            }
            catch (Exception) {
                throw;
            }
            finally {
                _eventPublishedVersionStore.AddOrUpdatePublishedVersion(sourceKey, stream.StartVersion, stream.EndVersion);
            }
        }

        private object Deserialize(EventStream.Stream stream)
        {
            return _serializer.Deserialize(stream.Payload, stream.GetSourceType());
        }

        public void Handle(EventStream stream)
        {
            if (stream.Events.IsEmpty()) {
                return;
            }

            List<Exception> innerExceptions = new List<Exception>();
            stream.Events.Select(Deserialize).OfType<IEvent>().ForEach(@event => {
                try {
                    _executor.Execute(@event);
                }
                catch (Exception ex) {
                    innerExceptions.Add(ex);
                }
            });

            switch (innerExceptions.Count) {
                case 0:
                    break;
                case 1:
                    throw innerExceptions[0];
                default:
                    throw new AggregateException(innerExceptions);
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
