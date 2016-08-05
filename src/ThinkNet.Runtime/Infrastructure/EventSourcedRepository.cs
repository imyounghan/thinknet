using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkNet.EventSourcing;
using ThinkNet.Messaging;


namespace ThinkNet.Infrastructure
{
    internal class EventSourcedRepository : IEventSourcedRepository
    {
        private readonly IEventStore _eventStore;
        private readonly ISnapshotStore _snapshotStore;
        private readonly ISnapshotPolicy _snapshotPolicy;
        private readonly ICache _cache;
        private readonly IEventBus _eventBus;
        private readonly IBinarySerializer _binarySerializer;
        private readonly ITextSerializer _textSerializer;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventSourcedRepository(IEventStore eventStore,
            ISnapshotStore snapshotStore,
            ISnapshotPolicy snapshotPolicy,
            ICache cache,
            IEventBus eventBus,
            IBinarySerializer binarySerializer,
            ITextSerializer textSerializer)
        {
            this._eventStore = eventStore;
            this._snapshotStore = snapshotStore;
            this._snapshotPolicy = snapshotPolicy;
            this._cache = cache;
            this._eventBus = eventBus;
            this._binarySerializer = binarySerializer;
            this._textSerializer = textSerializer;
        }

        public IEventSourced Create(Type type, object id)
        {
            var idType = id.GetType();
            ConstructorInfo constructor = type.GetConstructor(new[] { idType });

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
                var snapshot = _snapshotStore.GetLastest(sourceKey);
                if (snapshot != null) {
                    eventSourced = _binarySerializer.Deserialize(snapshot.Payload, eventSourcedType) as IEventSourced;

                    if (LogManager.Default.IsDebugEnabled)
                        LogManager.Default.DebugFormat("find the aggregate root {0} of id {1} from snapshot. version:{2}.",
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

            var streams = _eventStore.FindAll(sourceKey, eventSourced.Version)
                .Aggregate(new SortedDictionary<int, IList<IEvent>>(), (total, next) => {
                    var @event = Deserialize(next);
                    total.GetOrAdd(next.Version, () => new List<IEvent>()).Add(@event);
                    return total;
                });
            if (!streams.IsEmpty()) {
                foreach (var stream in streams) {
                    eventSourced.LoadFrom(stream.Key, stream.Value);
                }

                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("restore the aggregate root {0} of id {1} from event stream. version:{2} ~ {3}",
                        eventSourcedType.FullName, eventSourcedId, streams.Min(p => p.Key), streams.Max(p => p.Key));
            }

            _cache.Set(eventSourced, eventSourced.Id);

            return eventSourced;
        }

        private IEvent Deserialize(DataStream stream)
        {
            return (IEvent)_binarySerializer.Deserialize(stream.Payload, stream.GetSourceType());
        }

        private EventStream.Stream SerializeToStream(IEvent @event)
        {
            return new EventStream.Stream(@event.GetType()) {
                Payload = _textSerializer.Serialize(@event)
            };
        }

        private DataStream Serialize(DataKey sourceKey, IEventSourced aggregateRoot)
        {
            return new DataStream() {
                Key = sourceKey,
                Version = aggregateRoot.Version,
                Payload = _binarySerializer.Serialize(aggregateRoot)
            };
        }

        /// <summary>
        /// 保存聚合事件。
        /// </summary>
        public void Save(IEventSourced eventSourced, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId)) {
                if (LogManager.Default.IsWarnEnabled)
                    LogManager.Default.Warn("Not use command to modify the state of the aggregate root.");
            }

            var aggregateRootType = eventSourced.GetType();
            var aggregateRootId = eventSourced.Id;
            var events = new List<IEvent>(eventSourced.GetEvents());
            var key = new DataKey(aggregateRootId, aggregateRootType);

            var streams = events.Select(@event => new DataStream() {
                Key = new DataKey(@event.Id, @event.GetType()),
                Version = eventSourced.Version + 1,
                Payload = _binarySerializer.Serialize(@event)
            }).ToArray();

            if (_eventStore.Save(key, correlationId, streams)) {
                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("Domain events persistent completed. aggregateRootId:{0}, aggregateRootType:{1}, commandId:{2}.",
                        aggregateRootId, aggregateRootType.FullName, correlationId);

                eventSourced.ClearEvents();
                _cache.Set(eventSourced, eventSourced.Id);
            }
            else {
                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("The command generates events have been saved. aggregateRootId:{0}, aggregateRootType:{1}, commandId:{2}.",
                        aggregateRootId, aggregateRootType.FullName, correlationId);
            }

            var eventStream = new EventStream {
                SourceAssemblyName = key.AssemblyName,
                SourceNamespace = key.Namespace,
                SourceTypeName = key.TypeName,
                SourceId = key.SourceId,
                CommandId = correlationId,
                Version = eventSourced.Version,
                Events = events.Select(SerializeToStream).ToArray()
            };
            _eventBus.Publish(eventStream);


            var snapshot = Serialize(key, eventSourced);
            if (!_snapshotPolicy.ShouldbeCreateSnapshot(snapshot))
                return;

            try {
                if (_snapshotStore.Save(snapshot) && LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("make snapshot completed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                       aggregateRootId, aggregateRootType.FullName, eventSourced.Version);
            }
            catch (Exception ex) {
                if (LogManager.Default.IsWarnEnabled)
                    LogManager.Default.Warn(ex,
                        "snapshot persistent failed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                        aggregateRootId, aggregateRootType.FullName, eventSourced.Version);
            }
        }
                
        ///// <summary>
        ///// 删除该聚合根下的溯源事件
        ///// </summary>
        //public void Delete(IEventSourced aggregateRoot)
        //{
        //    this.Delete(aggregateRoot.GetType(), aggregateRoot.Id);
        //}

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
