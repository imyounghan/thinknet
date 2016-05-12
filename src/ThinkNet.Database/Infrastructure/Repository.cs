using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Database;
using ThinkNet.Kernel;
using ThinkNet.Messaging;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkLib.Serialization;

namespace ThinkNet.Infrastructure
{
    [RegisterComponent(typeof(IRepository))]
    public sealed class Repository : IRepository
    {
        private readonly IDataContextFactory _dbContextFactory;
        private readonly IEventBus _eventBus;
        private readonly IMemoryCache _cache;
        private readonly ITextSerializer _serializer;
        private readonly ILogger _logger;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public Repository(IDataContextFactory dbContextFactory, IEventBus eventBus, IMemoryCache cache, ITextSerializer serializer)
        {
            this._dbContextFactory = dbContextFactory;
            this._eventBus = eventBus;
            this._cache = cache;
            this._serializer = serializer;
            this._logger = LogManager.GetLogger("ThinkZoo");
        }

        #region IRepository 成员

        public TAggregateRoot Find<TAggregateRoot, TKey>(TKey key) where TAggregateRoot : class, IAggregateRoot
        {
            var aggregateRootType = typeof(TAggregateRoot);
            var aggregateRoot = (TAggregateRoot)_cache.Get(aggregateRootType, key);

            if (aggregateRoot == null) {
                aggregateRoot = this.LoadFromStorage<TAggregateRoot>(key);

                if (aggregateRoot != null && _logger.IsDebugEnabled) {
                    _logger.DebugFormat("find the aggregate root '{0}' of id '{1}' from storage.",
                        aggregateRootType.FullName, key);
                }
            }
            else {
                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("find the aggregate root '{0}' of id '{1}' from cache.",
                        aggregateRootType.FullName, key);
            }

            return aggregateRoot;
        }

        private T LoadFromStorage<T>(object id) where T : class
        {
            var task = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    var entity = context.Find<T>(id);
                    if (entity != null) {
                        _cache.Set(entity, id);
                    }

                    return entity;
                }
            });
            task.Wait();

            return task.Result;
        }

        public void Save<TAggregateRoot>(TAggregateRoot aggregateRoot, string correlationId) where TAggregateRoot : class, IAggregateRoot
        {
            Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    context.SaveOrUpdate(aggregateRoot);
                    context.Commit();
                }               

                _cache.Set(aggregateRoot, aggregateRoot.Id);
            }).Wait();
            

            var eventPublisher = aggregateRoot as IEventPublisher;
            if (eventPublisher == null) {
                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("The aggregate root {0} of id {1} is saved.",
                        typeof(TAggregateRoot).FullName, aggregateRoot.Id);
                
                return;
            }
            

            var events = eventPublisher.Events;
            if (string.IsNullOrWhiteSpace(correlationId)) {
                _eventBus.Publish(events);
            }
            else {
                _eventBus.Publish(new EventStream(aggregateRoot.Id, typeof(TAggregateRoot)) {
                    CommandId = correlationId,
                    Events = events.Select(item => new EventStream.Stream(item.GetType()) {
                        Payload = _serializer.Serialize(item)
                    }).ToArray()
                });
            }

            if (_logger.IsDebugEnabled)
                _logger.DebugFormat("The aggregate root {0} of id {1} is saved then publish all events [{2}].",
                    typeof(TAggregateRoot).FullName, aggregateRoot.Id,
                    string.Join("|", events.Select(item => _serializer.Serialize(item)).ToArray()));
        }

        public void Delete<TAggregateRoot>(TAggregateRoot aggregateRoot) where TAggregateRoot : class, IAggregateRoot
        {
            Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    context.Delete(aggregateRoot);
                    context.Commit();
                }

                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("The aggregate root {0} of id {1} is deleted.",
                        typeof(TAggregateRoot).FullName, aggregateRoot.Id);

                _cache.Remove(typeof(TAggregateRoot), aggregateRoot.Id);
            }).Wait();
        }

        public void Delete<TAggregateRoot, TKey>(TKey key) where TAggregateRoot : class, IAggregateRoot
        {
            Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    var aggregateRoot = context.Find<TAggregateRoot>(key);
                    if (aggregateRoot != null) {
                        context.Delete(aggregateRoot);
                        context.Commit();
                    }
                }

                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("The aggregate root {0} of id {1} is deleted.",
                        typeof(TAggregateRoot).FullName, key);

                _cache.Remove(typeof(TAggregateRoot), key);
            }).Wait();
        }

        #endregion
    }
}
