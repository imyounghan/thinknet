using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThinkLib.Scheduling;
using ThinkLib.Serialization;
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
        private readonly ICommandResultManager _commandResultManager;
        private readonly ITextSerializer _serializer;

        private readonly BlockingCollection<VersionedEventStream> queue;
        private readonly Worker worker;

        public EventStreamHandler(IMessageExecutor executor,
            IEventPublishedVersionStore eventPublishedVersionStore,
            ITextSerializer serializer,
            ICommandResultManager commandResultManager)
        {
            this._executor = executor;
            this._eventPublishedVersionStore = eventPublishedVersionStore;
            this._serializer = serializer;
            this._commandResultManager = commandResultManager;

            //this.worker = WorkerFactory.Create(Retry);
            //this.queue = new BlockingCollection<VersionedEventStream>();
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
                else
                    _commandResultManager.NotifyCommandCompleted(stream.CommandId, CommandStatus.Success);
                return;
            }

            try {
                this.Handle(stream as EventStream);
            }
            catch (Exception ex) {
                _commandResultManager.NotifyCommandCompleted(stream.CommandId, CommandStatus.Failed, ex);
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
                _commandResultManager.NotifyCommandCompleted(stream.CommandId, CommandStatus.NothingChanged);
                return;
            }

            List<Exception> exceptions = new List<Exception>();
            stream.Events.Select(Deserialize).OfType<IEvent>().ForEach(@event => {
                try {
                    _executor.Execute(@event);
                }
                catch (Exception ex) {
                    exceptions.Add(ex);
                }
            });

            _commandResultManager.NotifyCommandCompleted(stream.CommandId,
                exceptions.Count > 0 ? CommandStatus.Failed : CommandStatus.Success,
                new AggregateException(exceptions));


            //try {
            //    @event.Events.ForEach(_executor.Execute);
            //}
            //catch (Exception ex) {
            //    exception = ex;
            //    throw ex;
            //}
            //finally {
            //    if (!string.IsNullOrWhiteSpace(@event.CommandId))
            //        _commandResultManager.NotifyCommandCompleted(@event.CommandId,
            //            exception != null ? CommandStatus.Failed : CommandStatus.Success,
            //            exception);
            //}
        }
    }
}
