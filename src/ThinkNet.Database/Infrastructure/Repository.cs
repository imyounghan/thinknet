using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Database;
using ThinkNet.Kernel;
using ThinkNet.Messaging;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkLib.Serialization;
using System;

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

        public IAggregateRoot Find(Type aggregateRootType, object id)
        {
            var aggregateRoot = _cache.Get(aggregateRootType, id);

            if (aggregateRoot == null) {
                aggregateRoot = this.LoadFromStorage(aggregateRootType, id);
                if (aggregateRoot != null) {
                    _cache.Set(aggregateRoot, id);
                }

                if (aggregateRoot != null && _logger.IsDebugEnabled) {
                    _logger.DebugFormat("find the aggregate root '{0}' of id '{1}' from storage.",
                        aggregateRootType.FullName, id);
                }
            }
            else {
                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("find the aggregate root '{0}' of id '{1}' from cache.",
                        aggregateRootType.FullName, id);
            }

            return aggregateRoot as IAggregateRoot;
        }

        private object LoadFromStorage(Type type, object id)
        {
            return Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    return context.Find(type, id);
                }
            }).Result;
        }

        //private object LoadFromStorage(Type type, object id)
        //{
        //    var task = Task.Factory.StartNew(() => {
        //        using (var context = _dbContextFactory.CreateDataContext()) {
        //            var entity = context.Find(type, id);
        //            if (entity != null) {
        //                _cache.Set(entity, id);
        //            }

        //            return entity;
        //        }
        //    });
        //    task.Wait();

        //    return task.Result;
        //}

        public void Save(IAggregateRoot aggregateRoot, string correlationId)
        {
            Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    context.SaveOrUpdate(aggregateRoot);
                    context.Commit();
                }               

                _cache.Set(aggregateRoot, aggregateRoot.Id);
            }).Wait();


            var aggregateRootType = aggregateRoot.GetType();
            var eventPublisher = aggregateRoot as IEventPublisher;
            if (eventPublisher == null) {
                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("The aggregate root {0} of id {1} is saved.",
                        aggregateRootType.FullName, aggregateRoot.Id);
                
                return;
            }
            

            var events = eventPublisher.Events;
            if (string.IsNullOrWhiteSpace(correlationId)) {
                _eventBus.Publish(events);
            }
            else {
                _eventBus.Publish(new EventStream(aggregateRoot.Id, aggregateRootType) {
                    CommandId = correlationId,
                    Events = events.Select(item => new EventStream.Stream(item.GetType()) {
                        Payload = _serializer.Serialize(item)
                    }).ToArray()
                });
            }

            if (_logger.IsDebugEnabled)
                _logger.DebugFormat("The aggregate root {0} of id {1} is saved then publish all events [{2}].",
                    aggregateRootType.FullName, aggregateRoot.Id,
                    string.Join("|", events.Select(item => _serializer.Serialize(item)).ToArray()));
        }

        public void Delete(IAggregateRoot aggregateRoot)
        {
            var aggregateRootType = aggregateRoot.GetType();
            _cache.Remove(aggregateRootType, aggregateRoot.Id);

            Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    context.Delete(aggregateRoot);
                    context.Commit();
                }

                if (_logger.IsDebugEnabled)
                    _logger.DebugFormat("The aggregate root {0} of id {1} is deleted.",
                        aggregateRootType, aggregateRoot.Id);
            }).Wait();
        }

        #endregion
    }
}
