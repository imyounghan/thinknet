using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinkLib.Common;
using ThinkLib.Logging;
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
        private readonly ILogger _logger;
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public Repository(IDataContextFactory dbContextFactory, IEventBus eventBus, IMemoryCache cache)
        {
            this._dbContextFactory = dbContextFactory;
            this._eventBus = eventBus;
            this._cache = cache;
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

        public void Save(IAggregateRoot aggregateRoot)
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
            if (events.IsEmpty())
                return;

            _eventBus.Publish(events);

            if (_logger.IsDebugEnabled)
                _logger.DebugFormat("The aggregate root {0} of id {1} is saved then publish all events [{2}].",
                    aggregateRootType.FullName, aggregateRoot.Id,
                    string.Join("|", events.Select(item => item.ToString())));
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
            }).Wait();

            if (_logger.IsDebugEnabled)
                _logger.DebugFormat("The aggregate root {0} of id {1} is deleted.",
                    aggregateRootType, aggregateRoot.Id);
        }

        #endregion
    }
}
