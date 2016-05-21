using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.ServiceLocation;
using ThinkLib.Common;
using ThinkLib.Contexts;
using ThinkLib.Scheduling;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;


namespace ThinkNet.Kernel
{
    internal class EventStreamHandler : IInitializer,
        IMessageHandler<EventStream>
    {
        enum SynchronizeStatus
        {
            Pass,
            Complete,
            Retry
        }

        private readonly IMessageExecutor _executor;
        private readonly IEventContextFactory _eventContextFactory; 
        private readonly IEventPublishedVersionStore _eventPublishedVersionStore;
        private readonly ITextSerializer _serializer;
        private readonly IMessageNotification _notification;

        private readonly BlockingCollection<EventStream> queue;
        private readonly Worker worker;

        public EventStreamHandler(IMessageExecutor executor,
            IEventPublishedVersionStore eventPublishedVersionStore,
            ITextSerializer serializer,
            IEventContextFactory eventContextFactory,
            IMessageNotification notification)
        {
            this._executor = executor;
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
            }
            catch (Exception ex) {
                _notification.NotifyMessageCompleted(stream.CommandId, ex);
                throw;
            }


            try {
                ExecuteAll(events);
            }
            catch (Exception) {
                throw;
            }
        }

        private SynchronizeStatus Synchronize(EventStream stream, IEnumerable<IVersionedEvent> events)
        {
            var actions = events.Select(GetEventHandleInfo).Where(p => !p.IsNull());
            if (actions.IsEmpty())
                return SynchronizeStatus.Pass;

            var sourceKey = new SourceKey(stream.SourceId, stream.SourceNamespace, stream.SourceTypeName, stream.SourceAssemblyName);
            var version = _eventPublishedVersionStore.GetPublishedVersion(sourceKey);

            if (version + 1 != stream.StartVersion) { //如果当前的消息版本不是要处理的情况
                if (stream.StartVersion > version + 1) //如果该消息的版本大于要处理的版本则重新进队列等待下次处理
                    queue.TryAdd(stream, 5000, worker.CancellationToken);
                return SynchronizeStatus.Retry;
            }

            CurrentContext.Bind(_eventContextFactory.CreateEventContext() as IContext);
            actions.ForEach(p => p.Item1.Handle(p.Item2));
            using (CurrentContext.Unbind(_eventContextFactory) as IDisposable) { }

            _eventPublishedVersionStore.AddOrUpdatePublishedVersion(sourceKey, stream.StartVersion, stream.EndVersion);


            return SynchronizeStatus.Complete;
        }

        private Tuple<IProxyHandler, IVersionedEvent> GetEventHandleInfo(IVersionedEvent @event)
        {
            var eventType = @event.GetType();
            var eventHandlers = GetHandlers(eventType);

            switch (eventHandlers.Count()) {
                case 0:
                    return null;
                case 1:
                    return new Tuple<IProxyHandler, IVersionedEvent>(eventHandlers.First(), @event);
                default:
                    throw new MessageHandlerTooManyException(eventType);
            }
        }

        private object Deserialize(EventStream.Stream stream)
        {
            return _serializer.Deserialize(stream.Payload, stream.GetSourceType());
        }

        private void ExecuteAll(IEnumerable<IVersionedEvent> events)
        {
            List<Exception> innerExceptions = new List<Exception>();

            foreach (var @event in events) {
                try {
                    _executor.Execute(@event);
                }
                catch (Exception ex) {
                    innerExceptions.Add(ex);
                }
            }

            switch (innerExceptions.Count) {
                case 0:
                    break;
                case 1:
                    throw innerExceptions[0];
                default:
                    throw new AggregateException(innerExceptions);
            }
        }


        private IEnumerable<IProxyHandler> GetHandlers(Type type)
        {
            var    handlerType = typeof(IEventHandler<>).MakeGenericType(type);
            var    handlerWrapperType = typeof(EventHandlerWrapper<>).MakeGenericType(type);
            return ServiceLocator.Current.GetAllInstances(handlerType)
                .Select(handler => Activator.CreateInstance(handlerWrapperType, new[] { handler, _eventContextFactory }))
                .Cast<IProxyHandler>()
                .AsEnumerable();
        }

        #region IInitializer 成员

        public void Initialize(IEnumerable<Type> types)
        {
            worker.Start();
        }

        #endregion
    }
}
