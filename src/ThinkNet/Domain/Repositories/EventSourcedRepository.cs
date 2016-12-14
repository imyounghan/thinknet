using System;
using System.Linq;
using System.Runtime.Serialization;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;


namespace ThinkNet.Domain.Repositories
{
    /// <summary>
    /// <see cref="IEventSourcedRepository"/> 的实现类
    /// </summary>
    public sealed class EventSourcedRepository : IEventSourcedRepository
    {
        private readonly IEventStore _eventStore;
        private readonly ICache _cache;
        private readonly ISnapshotStore _snapshotStore;
        private readonly ISnapshotPolicy _snapshotPolicy;
        private readonly IMessageBus _messageBus;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventSourcedRepository(IEventStore eventStore,
            ISnapshotStore snapshotStore,
            ISnapshotPolicy snapshotPolicy,
            IMessageBus messageBus,
            ICache cache)
        {
            this._eventStore = eventStore;
            this._snapshotStore = snapshotStore;
            this._snapshotPolicy = snapshotPolicy;
            this._messageBus = messageBus;
            this._cache = cache;
        }

        private static IEventSourced Create(Type aggregateRootType, object id)
        {
            var idType = id.GetType();
            var constructor = aggregateRootType.GetConstructor(new[] { idType });

            if (constructor == null) {
                //string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor({1} id)", type.FullName, idType.FullName);
                //throw new ThinkNetException(errorMessage);
                return FormatterServices.GetUninitializedObject(aggregateRootType) as IEventSourced;
            }

            return constructor.Invoke(new[] { id }) as IEventSourced;
        }

        private static bool IsEventSourced(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IEventSourced).IsAssignableFrom(type);
        }

        /// <summary>
        /// 根据主键获取聚合根实例。
        /// </summary>
        public IEventSourced Find(Type eventSourcedType, object eventSourcedId)
        {
            if (!IsEventSourced(eventSourcedType)) {
                string errorMessage = string.Format("The type of '{0}' does not extend interface IEventSourced.", eventSourcedType.FullName);
                if (LogManager.Default.IsErrorEnabled)
                    LogManager.Default.Error(errorMessage);
                throw new ThinkNetException(errorMessage);
            }

            IEventSourced eventSourced = null;
            if (_cache.TryGet(eventSourcedType, eventSourcedId, out eventSourced)) {
                if(LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("find the aggregate root {0} of id {1} from cache.",
                        eventSourcedType.FullName, eventSourcedId);

                return eventSourced;
            }
            //IEventSourced eventSourced = null;
            var sourceKey = new SourceKey(eventSourcedId, eventSourcedType);
            try {
                eventSourced = _snapshotStore.GetLastest<IEventSourced>(sourceKey);
                if (eventSourced != null) {
                    if (LogManager.Default.IsDebugEnabled)
                        LogManager.Default.DebugFormat("Find the aggregate root '{0}' of id '{1}' from snapshot. current version:{2}.",
                            eventSourcedType.FullName, eventSourcedId, eventSourced.Version);
                }                
            }
            catch (Exception ex) {
                if (LogManager.Default.IsWarnEnabled)
                    LogManager.Default.Warn(ex,
                        "Get the latest snapshot failed. aggregateRootId:{0},aggregateRootType:{1}.",
                        eventSourcedId, eventSourcedType.FullName);
            }

            var events = _eventStore.FindAll(sourceKey, eventSourced.Version);
            if (!events.IsEmpty()) {
                if (eventSourced == null) {
                    eventSourced = Create(eventSourcedType, eventSourcedId);
                }
                foreach (var @event in events) {
                    if (@event.Version != eventSourced.Version + 1) {
                        var errorMessage = string.Format("Cannot load because the version '{0}' is not equal to the AggregateRoot version '{1}' on '{2}' of id '{3}'.",
                            @event.Version, eventSourced.Version, eventSourcedType.FullName, eventSourcedId);
                        throw new ThinkNetException(errorMessage);
                    }
                    eventSourced.LoadFrom(@event);
                }                

                if(LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("Restore the aggregate root '{0}' of id '{1}' from event stream. version:{2} ~ {3}",
                        eventSourcedType.FullName, eventSourcedId, events.Min(p => p.Version), events.Max(p => p.Version));
            }

            _cache.Set(eventSourced, eventSourcedId);

            return eventSourced;
        }

        /// <summary>
        /// 保存聚合事件。
        /// </summary>
        public void Save(IEventSourced eventSourced, string correlationId)
        {
            if(string.IsNullOrWhiteSpace(correlationId)) {
                if(LogManager.Default.IsWarnEnabled)
                    LogManager.Default.Warn("Not use command to modify the state of the aggregate root.");
            }

            var aggregateRootType = eventSourced.GetType();

            var eventCollection = new EventCollection(eventSourced.Events) {
                CorrelationId = correlationId,
                SourceId = new SourceKey(eventSourced.Id, aggregateRootType),
                Version = eventSourced.Version
            };

            try {
                _eventStore.Save(eventCollection);

                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("Domain events persistent completed. aggregateRootId:{0}, aggregateRootType:{1}, commandId:{2}.",
                        eventSourced.Id, aggregateRootType.FullName, correlationId);
            }
            catch (Exception ex) {
                if(LogManager.Default.IsErrorEnabled)
                    LogManager.Default.Error(ex,
                        "Domain events persistent failed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                        eventSourced.Id, aggregateRootType.FullName, eventSourced.Version);
                throw ex;
            }

            _cache.Set(eventSourced, eventSourced.Id);
            _messageBus.PublishAsync((IMessage)eventCollection);


            if(!_snapshotPolicy.ShouldbeCreateSnapshot(eventSourced))
                return;

            _snapshotStore.Save(eventSourced);
        }
                
        /// <summary>
        /// 删除该聚合根下的溯源事件
        /// </summary>
        public void Delete(Type eventSourcedType, object eventSourcedId)
        {
            _cache.Remove(eventSourcedType, eventSourcedId);

            var key = new SourceKey(eventSourcedId, eventSourcedType);
            _snapshotStore.Remove(key);
            _eventStore.RemoveAll(key);            
        }
    }
}
