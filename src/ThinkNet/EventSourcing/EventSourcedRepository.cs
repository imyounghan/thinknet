using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using ThinkNet.EventSourcing;
using ThinkNet.Messaging;


namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// <see cref="IEventSourcedRepository"/> 的实现类
    /// </summary>
    public sealed class EventSourcedRepository : IEventSourcedRepository
    {
        private readonly IEventStore _eventStore;
        private readonly ISnapshotStore _snapshotStore;
        private readonly ISnapshotPolicy _snapshotPolicy;
        private readonly ICache _cache;
        private readonly IEventBus _eventBus;
        private readonly ConcurrentDictionary<Type, ConstructorInfo> _typeConstructors;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventSourcedRepository(IEventStore eventStore,
            ISnapshotStore snapshotStore,
            ISnapshotPolicy snapshotPolicy,
            ICache cache,
            IEventBus eventBus)
        {
            this._eventStore = eventStore;
            this._snapshotStore = snapshotStore;
            this._snapshotPolicy = snapshotPolicy;
            this._cache = cache;
            this._eventBus = eventBus;
            this._typeConstructors = new ConcurrentDictionary<Type, ConstructorInfo>();
        }

        private IEventSourced Create(Type type, object id)
        {
            var idType = id.GetType();
            var constructor = _typeConstructors.GetOrAdd(type, key => type.GetConstructor(new[] { id.GetType() }));

            if (constructor == null) {
                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor({1} id)", type.FullName, idType.FullName);
                throw new EventSourcedException(errorMessage);
            }

            return constructor.Invoke(new[] { id }) as IEventSourced;
        }

        /// <summary>
        /// 根据主键获取聚合根实例。
        /// </summary>
        public IEventSourced Find(Type eventSourcedType, object eventSourcedId)
        {
            if (!TypeHelper.IsEventSourced(eventSourcedType)) {
                string errorMessage = string.Format("The type of '{0}' does not extend interface IEventSourced.", eventSourcedType.FullName);
                if (LogManager.Default.IsErrorEnabled)
                    LogManager.Default.Error(errorMessage);
                throw new EventSourcedException(errorMessage);
            }

            var eventSourced = _cache.Get(eventSourcedType, eventSourcedId) as IEventSourced;
            if (eventSourced != null) {
                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("find the aggregate root {0} of id {1} from cache.",
                        eventSourcedType.FullName, eventSourcedId);

                return eventSourced;
            }

            var sourceKey = new DataKey(eventSourcedId, eventSourcedType);
            try {
                eventSourced = _snapshotStore.GetLastest(sourceKey);
                if (eventSourced != null) {
                    if (LogManager.Default.IsDebugEnabled)
                        LogManager.Default.DebugFormat("find the aggregate root {0} of id {1} from snapshot. current version:{2}.",
                            eventSourcedType.FullName, eventSourcedId, eventSourced.Version);
                }                
            }
            catch (Exception ex) {
                if (LogManager.Default.IsWarnEnabled)
                    LogManager.Default.Warn(ex,
                        "get the latest snapshot failed. aggregateRootId:{0},aggregateRootType:{1}.",
                        eventSourcedId, eventSourcedType.FullName);
            }

            if (eventSourced == null) {
                eventSourced = this.Create(eventSourcedType, eventSourcedId);
            }

            var events = _eventStore.FindAll(sourceKey, eventSourced.Version);
            if (!events.IsEmpty()) {
                eventSourced.LoadFrom(events);

                if(LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("restore the aggregate root {0} of id {1} from event stream. version:{2} ~ {3}",
                        eventSourcedType.FullName, eventSourcedId, events.Min(p => p.Version), events.Max(p => p.Version));
            }

            _cache.Set(eventSourced, eventSourced.Id);

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
            var aggregateRootId = eventSourced.Id.ToString();

            var @event = new VersionedEvent() {
                CommandId = correlationId,
                Events = eventSourced.GetEvents(),
                SourceId = aggregateRootId,
                SourceType = aggregateRootType,
                Version = eventSourced.Version + 1
            };

            try {
                _eventStore.Save(@event);

                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("events persistent completed. aggregateRootId:{0}, aggregateRootType:{1}, commandId:{2}.",
                        aggregateRootId, aggregateRootType.FullName, correlationId);
            }
            catch (Exception ex) {
                if(LogManager.Default.IsErrorEnabled)
                    LogManager.Default.Error(ex,
                        "events persistent failed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                        aggregateRootId, aggregateRootType.FullName, eventSourced.Version);
                throw ex;
            }

            eventSourced.ClearEvents();
            _cache.Set(eventSourced, eventSourced.Id);
            _eventBus.Publish(@event);


            if(!_snapshotPolicy.ShouldbeCreateSnapshot(eventSourced))
                return;

            _snapshotStore.Save(eventSourced);
        }
                
        /// <summary>
        /// 删除该聚合根下的溯源事件
        /// </summary>
        public void Delete(Type aggregateRootType, object aggregateRootId)
        {
            _cache.Remove(aggregateRootType, aggregateRootId);

            var key = new DataKey(aggregateRootId, aggregateRootType);
            _snapshotStore.Remove(key);
            _eventStore.RemoveAll(key);            
        }
    }
}
