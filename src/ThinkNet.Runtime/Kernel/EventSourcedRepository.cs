using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkLib.Serialization;


namespace ThinkNet.Kernel
{
    /// <summary>
    /// <see cref="IRepository"/> 的实现
    /// </summary>
    [RegisterComponent(typeof(IEventSourcedRepository))]
    public class EventSourcedRepository : IEventSourcedRepository
    {
        private readonly IEventStore _eventStore;
        private readonly ISnapshotStore _snapshotStore;
        private readonly ISnapshotPolicy _snapshotPolicy;
        private readonly IMemoryCache _cache;
        private readonly IEventBus _eventBus;
        private readonly IAggregateRootFactory _aggregateFactory;
        private readonly IBinarySerializer _binarySerializer;
        private readonly ITextSerializer _textSerializer;
        private readonly ILogger _logger;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventSourcedRepository(IEventStore eventStore,
            ISnapshotStore snapshotStore,
            ISnapshotPolicy snapshotPolicy,
            IMemoryCache cache,
            IEventBus eventBus,
            IAggregateRootFactory aggregateFactory,
            IBinarySerializer binarySerializer,
            ITextSerializer textSerializer)
        {
            this._eventStore = eventStore;
            this._snapshotStore = snapshotStore;
            this._snapshotPolicy = snapshotPolicy;
            this._cache = cache;
            this._eventBus = eventBus;
            this._aggregateFactory = aggregateFactory;
            this._binarySerializer = binarySerializer;
            this._textSerializer = textSerializer;
            this._logger = LogManager.GetLogger("ThinkZoo");
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
                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("find the aggregate root {0} of id {1} from cache.",
                        aggregateRootType.FullName, aggregateRootId);

                return aggregateRoot;
            }

            var sourceKey = new SourceKey(aggregateRootId, aggregateRootType);
            try {
                var snapshot = _snapshotStore.GetLastest(sourceKey);
                if (snapshot != null) {
                    aggregateRoot = _binarySerializer.Deserialize(snapshot.Payload, aggregateRootType) as IEventSourced;

                    if (_logger.IsDebugEnabled)
                        _logger.DebugFormat("find the aggregate root {0} of id {1} from snapshot. version:{2}.",
                            aggregateRootType.FullName, aggregateRootId, aggregateRoot.Version);
                }                
            }
            catch (Exception ex) {
                if (_logger.IsWarnEnabled)
                    _logger.Warn(ex,
                        "get the latest snapshot failed. aggregateRootId:{0},aggregateRootType:{1}.",
                        aggregateRootId, aggregateRootType.FullName);
            }

            if (aggregateRoot == null) {
                aggregateRoot = _aggregateFactory.Create(aggregateRootType, aggregateRootId) as IEventSourced;
            }

            var events = _eventStore.FindAll(sourceKey, aggregateRoot.Version).Select(Deserialize).OfType<IVersionedEvent>().OrderBy(p => p.Version);
            if (!events.IsEmpty()) {
                aggregateRoot.LoadFrom(events);

                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("restore the aggregate root {0} of id {1} from events. version:{2} ~ {3}",
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
                Events = events.Select(item => new EventStream.Stream(item.GetType()) {
                    Payload = _textSerializer.Serialize(item)
                }).ToArray()
            };
        }

        /// <summary>
        /// 保存聚合事件。
        /// </summary>
        public void Save(IEventSourced aggregateRoot, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId)) {
                if (_logger.IsWarnEnabled)
                    _logger.Warn("Not use command to modify the state of the aggregate root.");
            }

            var aggregateRootType = aggregateRoot.GetType();
            var aggregateRootId = aggregateRoot.Id;
            var events = aggregateRoot.GetEvents();
            var key = new SourceKey(aggregateRootId, aggregateRootType);            

            if (!_eventStore.EventPersisted(key, correlationId)) {
                _eventStore.Save(key, correlationId, events.Select(Serialize));

                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("Domain events persistent completed. aggregateRootId:{0}, aggregateRootType:{1}, commandId:{2}, events:[{3}].",
                        aggregateRootId, aggregateRootType.FullName, correlationId,
                        string.Join("|", events.Select(item => _textSerializer.Serialize(item)).ToArray()));
            }
            else {
                events = _eventStore.FindAll(key, correlationId).Select(Deserialize).OfType<IVersionedEvent>().OrderBy(p => p.Version);

                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("The command generates events have been saved, load from storage. aggregateRootId:{0}, aggregateRootType:{1}, commandId:{2}, events:[{3}].",
                        aggregateRootId, aggregateRootType.FullName, correlationId,
                        string.Join("|", events.Select(item => _textSerializer.Serialize(item)).ToArray()));
            }

            if (string.IsNullOrWhiteSpace(correlationId)) {
                _eventBus.Publish(events);
            }
            else {
                _eventBus.Publish(Convert(key, correlationId, events));
            }

            var eventPublisher = aggregateRoot as IEventPublisher;
            if (eventPublisher != null) {
                eventPublisher.Events.ForEach(item => {
                    if (item is IVersionedEvent)
                        return;
                });
            }

            _cache.Set(aggregateRoot, aggregateRoot.Id);

            var snapshot = Serialize(key, aggregateRoot);
            if (!_snapshotPolicy.ShouldbeCreateSnapshot(snapshot))
                return;

            try {
                if (_snapshotStore.Save(snapshot) && _logger.IsDebugEnabled)
                    _logger.DebugFormat("make snapshot completed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                       aggregateRootId, aggregateRootType.FullName, aggregateRoot.Version);
            }
            catch (Exception ex) {
                if (_logger.IsWarnEnabled)
                    _logger.Warn(ex,
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
