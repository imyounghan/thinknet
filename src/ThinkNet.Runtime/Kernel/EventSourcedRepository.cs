using System;
using System.Collections.Generic;
using System.Linq;
using ThinkLib.Logging;
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
        private readonly ITextSerializer _textSerializer;
        private readonly IBinarySerializer _binarySerializer;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventSourcedRepository(IEventStore eventStore,
            ISnapshotStore snapshotStore,
            ISnapshotPolicy snapshotPolicy,
            IMemoryCache cache,
            IEventBus eventBus,
            IAggregateRootFactory aggregateFactory)
        {
            this._eventStore = eventStore;
            this._snapshotStore = snapshotStore;
            this._snapshotPolicy = snapshotPolicy;
            this._cache = cache;
            this._eventBus = eventBus;
            this._aggregateFactory = aggregateFactory;
        }

        /// <summary>
        /// 根据主键获取聚合根实例。
        /// </summary>
        public TAggregateRoot Find<TAggregateRoot>(object aggregateRootId)
            where TAggregateRoot : class, IEventSourced
        {
            var aggregateRootType = typeof(TAggregateRoot);
            var aggregateRoot = _cache.Get(aggregateRootType, aggregateRootId) as TAggregateRoot;

            if (aggregateRoot != null) {
                LogManager.GetLogger("ThinkNet").InfoFormat("find the aggregate root {0} of id {1} from cache.",
                    aggregateRootType.FullName, aggregateRootId);

                return aggregateRoot;
            }

            var sourceKey = new SourceKey(aggregateRootId.ToString(), aggregateRootType);
            try {
                var snapshot = _snapshotStore.GetLastest(sourceKey);
                if (snapshot != null) {
                    aggregateRoot = _binarySerializer.Deserialize(snapshot.Item2, aggregateRootType) as TAggregateRoot;
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
                aggregateRoot = _aggregateFactory.Create<TAggregateRoot>(aggregateRootId);
            }

            var events = _eventStore.FindAll(sourceKey, aggregateRoot.Version).Select(_textSerializer.Serialize).Cast<IVersionedEvent>().OrderBy(p => p.Version);
            if (!events.IsEmpty()) {
                aggregateRoot.LoadFrom(events);
                LogManager.GetLogger("ThinkNet").InfoFormat("restore the aggregate root {0} of id {1} from events. version:{2} ~ {3}",
                        aggregateRootType.FullName, aggregateRootId, events.Min(p => p.Version), events.Max(p => p.Version));
            }

            _cache.Set(aggregateRoot, aggregateRoot.Id);

            return aggregateRoot;
        }


        private VersionedEventStream Convert(SourceKey source, string correlationId, IEnumerable<IVersionedEvent> events)
        {
            return new VersionedEventStream {
                AggregateRoot = source,
                CommandId = correlationId,
                StartVersion = events.Min(item => item.Version),
                EndVersion = events.Max(item => item.Version),
                Events = events
            };
        }

        /// <summary>
        /// 保存聚合事件。
        /// </summary>
        public void Save<TAggregateRoot>(TAggregateRoot aggregateRoot, string correlationId)
            where TAggregateRoot : class, IEventSourced
        {
            if (string.IsNullOrWhiteSpace(correlationId)) {
                LogManager.GetLogger("ThinkNet").Warn("Not use command to modify the state of the aggregate root.");
            }

            Type aggregateRootType = aggregateRoot.GetType();
            string aggregateRootId = aggregateRoot.Id.ToString();
            IEnumerable<IVersionedEvent> events = aggregateRoot.GetEvents();
            var key = new SourceKey(aggregateRootId, aggregateRootType);

            if (!_eventStore.EventPersisted(key, correlationId)) {
                _eventStore.Save(key, correlationId, events.ToDictionary(item => item.Version, item => _textSerializer.Serialize(item)));
                LogManager.GetLogger("ThinkNet").InfoFormat("sourcing events persistent completed. aggregateRootId:{0},aggregateRootType:{1}.",
                    aggregateRootId, aggregateRootType.FullName);
            }
            else {
                events = _eventStore.FindAll(key, correlationId).Select(_textSerializer.Deserialize).Cast<IVersionedEvent>().OrderBy(p => p.Version);
                LogManager.GetLogger("ThinkNet").InfoFormat("the command generates events have been saved, load from storage. command id:{0}", correlationId);
            }

            if (string.IsNullOrWhiteSpace(correlationId)) {
                _eventBus.Publish(events);
            }
            else {
                _eventBus.Publish(Convert(key, correlationId, events));
            }
            LogManager.GetLogger("ThinkNet").InfoFormat("publish all events. event ids: [{0}]",
                string.Join(",", events.Select(@event => @event.Id).ToArray()));

            _cache.Set(aggregateRoot, aggregateRoot.Id);

            if (_snapshotPolicy.ShouldbeCreateSnapshot(aggregateRoot))
                return;

            try {
                _snapshotStore.Save(key, aggregateRoot.Version, _binarySerializer.Serialize(aggregateRoot));

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
        public void Delete<TAggregateRoot>(TAggregateRoot aggregateRoot) where TAggregateRoot : class, IEventSourced
        {
            Type aggregateRootType = aggregateRoot.GetType();
            string aggregateRootId = aggregateRoot.Id.ToString();

            var key = new SourceKey(aggregateRootId, aggregateRootType);

            _snapshotStore.Remove(key);
            _eventStore.RemoveAll(key);

            _cache.Remove(aggregateRootType, aggregateRootId);
        }
    }
}
