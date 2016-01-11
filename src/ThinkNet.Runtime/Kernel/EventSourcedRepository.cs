using System;
using System.Collections.Generic;
using System.Linq;
using ThinkLib.Logging;
using ThinkNet.Common;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;


namespace ThinkNet.Kernel
{
    /// <summary>
    /// <see cref="IRepository"/> 的实现
    /// </summary>
    [RegisterComponent(typeof(IRepository))]
    public class EventSourcedRepository : IEventSourcedRepository
    {
        private readonly IEventStore _eventStore;
        private readonly ISnapshotStore _snapshotStore;
        private readonly ISnapshotPolicy _snapshotPolicy;
        private readonly IMemoryCache _cache;
        private readonly IEventBus _eventBus;
        private readonly IAggregateRootFactory _aggregateFactory;
        private readonly IBinarySerializer _binarySerializer;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventSourcedRepository(IEventStore eventStore,
            ISnapshotStore snapshotStore,
            ISnapshotPolicy snapshotPolicy,
            IMemoryCache cache,
            IEventBus eventBus,
            IAggregateRootFactory aggregateFactory,
            IBinarySerializer binarySerializer)
        {
            this._eventStore = eventStore;
            this._snapshotStore = snapshotStore;
            this._snapshotPolicy = snapshotPolicy;
            this._cache = cache;
            this._eventBus = eventBus;
            this._aggregateFactory = aggregateFactory;
            this._binarySerializer = binarySerializer;
        }

        /// <summary>
        /// 根据主键获取聚合根实例。
        /// </summary>
        public IEventSourced Find(Type aggregateRootType, object aggregateRootId)
        {
            if (!aggregateRootType.IsAssignableFrom(typeof(IEventSourced))) {
            }

            var aggregateRoot = _cache.Get(aggregateRootType, aggregateRootId) as IEventSourced;

            if (aggregateRoot != null) {
                LogManager.GetLogger("ThinkNet").InfoFormat("find the aggregate root {0} of id {1} from cache.",
                    aggregateRootType.FullName, aggregateRootId);

                return aggregateRoot;
            }

            var sourceKey = new SourceKey(aggregateRootId, aggregateRootType);
            try {
                var snapshot = _snapshotStore.GetLastest(sourceKey);
                if (snapshot != null) {
                    aggregateRoot = _binarySerializer.Deserialize(snapshot.Payload, aggregateRootType) as IEventSourced;
                    LogManager.GetLogger("ThinkNet").InfoFormat("find the aggregate root {0} of id {1} from snapshot. version:{2}.",
                        aggregateRootType.FullName, aggregateRootId, aggregateRoot.Version);
                }                
            }
            catch (Exception ex) {
                LogManager.GetLogger("ThinkNet").Warn(ex,
                    "get the latest snapshot failed. aggregateRootId:{0},aggregateRootType:{1}.",
                    aggregateRootId, aggregateRootType.FullName);
            }

            if (aggregateRoot == null) {
                aggregateRoot = _aggregateFactory.Create(aggregateRootType, aggregateRootId) as IEventSourced;
            }

            var events = _eventStore.FindAll(sourceKey, aggregateRoot.Version).Select(Deserialize).OfType<IVersionedEvent>().OrderBy(p => p.Version);
            if (!events.IsEmpty()) {
                aggregateRoot.LoadFrom(events);
                LogManager.GetLogger("ThinkNet").InfoFormat("restore the aggregate root {0} of id {1} from events. version:{2} ~ {3}",
                        aggregateRootType.FullName, aggregateRootId, events.Min(p => p.Version), events.Max(p => p.Version));
            }

            _cache.Set(aggregateRoot, aggregateRoot.Id);

            return aggregateRoot;
        }

        private object Deserialize(Stream stream)
        {
            return _binarySerializer.Deserialize(stream.Payload, stream.GetSourceType());
        }

        private Stream Serialize(IVersionedEvent @event)
        {
            return new Stream() {
                Key = new SourceKey(@event.Id, @event.GetType()),
                Version = @event.Version,
                Payload = _binarySerializer.Serialize(@event)
            };
        }

        private Stream Serialize(SourceKey sourceKey, IEventSourced aggregateRoot)
        {
            return new Stream() {
                Key = sourceKey,
                Version = aggregateRoot.Version,
                Payload = _binarySerializer.Serialize(aggregateRoot)
            };
        }


        private VersionedEventStream Convert(SourceKey source, string correlationId, IEnumerable<IVersionedEvent> events)
        {
            return new VersionedEventStream {
                SourceAssemblyName = source.AssemblyName,
                SourceNamespace = source.Namespace,
                SourceTypeName = source.TypeName,
                SourceId = source.SourceId,
                CommandId = correlationId,
                StartVersion = events.Min(item => item.Version),
                EndVersion = events.Max(item => item.Version),
                Events = events
            };
        }

        /// <summary>
        /// 保存聚合事件。
        /// </summary>
        public void Save(IEventSourced aggregateRoot, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId)) {
                LogManager.GetLogger("ThinkNet").Warn("Not use command to modify the state of the aggregate root.");
            }

            Type aggregateRootType = aggregateRoot.GetType();
            object aggregateRootId = aggregateRoot.Id;
            IEnumerable<IVersionedEvent> events = aggregateRoot.GetEvents();
            var key = new SourceKey(aggregateRootId, aggregateRootType);

            if (!_eventStore.EventPersisted(key, correlationId)) {
                _eventStore.Save(key, correlationId, events.Select(Serialize));
                LogManager.GetLogger("ThinkNet").InfoFormat("sourcing events persistent completed. aggregateRootId:{0},aggregateRootType:{1}.",
                    aggregateRootId, aggregateRootType.FullName);
            }
            else {
                events = _eventStore.FindAll(key, correlationId).Select(Deserialize).OfType<IVersionedEvent>().OrderBy(p => p.Version);
                LogManager.GetLogger("ThinkNet").InfoFormat("the command generates events have been saved, load from storage. command id:{0}", correlationId);
            }

            if (string.IsNullOrWhiteSpace(correlationId)) {
                _eventBus.Publish(events);
            }
            else {
                _eventBus.Publish(Convert(key, correlationId, events));
            }
            LogManager.GetLogger("ThinkNet").InfoFormat("publish all events. event: [{0}]",
                string.Join("|", events.Select(@event => @event.ToString())));

            _cache.Set(aggregateRoot, aggregateRoot.Id);

            var snapshot = Serialize(key, aggregateRoot);
            if (_snapshotPolicy.ShouldbeCreateSnapshot(snapshot))
                return;

            try {
                _snapshotStore.Save(snapshot);

                LogManager.GetLogger("ThinkNet").InfoFormat("make snapshot completed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                   aggregateRootId, aggregateRootType.FullName, aggregateRoot.Version);
            }
            catch (Exception ex) {
                LogManager.GetLogger("ThinkNet").Warn(ex,
                    "snapshot persistent failed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                    aggregateRootId, aggregateRootType.FullName, aggregateRoot.Version);
            }
        }
                
        /// <summary>
        /// 删除该聚合根下的溯源事件
        /// </summary>
        public void Delete(IEventSourced aggregateRoot)
        {
            this.Delete(aggregateRoot.GetType(), aggregateRoot.Id);
        }

        /// <summary>
        /// 删除该聚合根下的溯源事件
        /// </summary>
        public void Delete(Type aggregateRootType, object aggregateRootId)
        {
            var key = new SourceKey(aggregateRootId, aggregateRootType);

            _snapshotStore.Remove(key);
            _eventStore.RemoveAll(key);

            _cache.Remove(aggregateRootType, aggregateRootId);
        }
    }
}
