using System.Linq;
using System.Threading.Tasks;
using ThinkLib.Logging;
using ThinkNet.Common;
using ThinkNet.Database;
using ThinkNet.Kernel;
using ThinkNet.Messaging;

namespace ThinkNet.Infrastructure
{
    [RegisterComponent(typeof(IRepository))]
    public sealed class Repository : IRepository
    {
        private readonly IDataContextFactory _dbContextFactory;
        private readonly IEventBus _eventBus;
        private readonly IMemoryCache _cache;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public Repository(IDataContextFactory dbContextFactory, IEventBus eventBus, IMemoryCache cache)
        {
            this._dbContextFactory = dbContextFactory;
            this._eventBus = eventBus;
            this._cache = cache;
        }

        #region IRepository 成员

        public TAggregateRoot Find<TAggregateRoot, TKey>(TKey key) where TAggregateRoot : class, IAggregateRoot
        {
            var aggregateRoot = (TAggregateRoot)_cache.Get(typeof(TAggregateRoot), key);

            if (aggregateRoot == null) {
                aggregateRoot = this.LoadFromStorage<TAggregateRoot>(key);

                if (aggregateRoot != null) {
                    LogManager.GetLogger("ThinkNet").InfoFormat("find the aggregate root '{0}' of id '{1}' from storage.",
                        typeof(TAggregateRoot).FullName, key);
                }
            }
            else {
                LogManager.GetLogger("ThinkNet").InfoFormat("find the aggregate root '{0}' of id '{1}' from cache.",
                        typeof(TAggregateRoot).FullName, key);
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
            if (eventPublisher == null)
                return;

            var events = eventPublisher.Events;
            if (string.IsNullOrWhiteSpace(correlationId)) {
                _eventBus.Publish(events);
            }
            else {
                _eventBus.Publish(new EventStream(aggregateRoot.Id, typeof(TAggregateRoot)) {
                    CommandId = correlationId,
                    Events = eventPublisher.Events
                });
            }
            LogManager.GetLogger("ThinkNet").InfoFormat("publish all events. events: [{0}]", string.Join("|",
                events.Select(@event => @event.ToString())));
        }

        public void Delete<TAggregateRoot>(TAggregateRoot aggregateRoot) where TAggregateRoot : class, IAggregateRoot
        {
            Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    context.Delete(aggregateRoot);
                    context.Commit();
                }

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

                _cache.Remove(typeof(TAggregateRoot), key);
            }).Wait();
        }

        #endregion
    }
}
