using System;
using System.Threading.Tasks;
using ThinkNet.Database;
using ThinkNet.Messaging;

namespace ThinkNet.Domain.Repositories
{
    /// <summary>
    /// <see cref="IRepository"/> 的实现类
    /// </summary>
    public sealed class Repository : IRepository
    {
        private readonly IDataContextFactory _dataContextFactory;
        private readonly IMessageBus _messageBus;
        private readonly ICache _cache;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public Repository(IDataContextFactory dataContextFactory, IMessageBus messageBus, ICache cache)
        {
            this._dataContextFactory = dataContextFactory;
            this._messageBus = messageBus;
            this._cache = cache;
        }


        private static bool IsAggregateRoot(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IAggregateRoot).IsAssignableFrom(type);
        }

        #region IRepository 成员
        /// <summary>
        /// 查找聚合。如果不存在返回null，存在返回实例
        /// </summary>
        public IAggregateRoot Find(Type aggregateRootType, object id)
        {
            if(!IsAggregateRoot(aggregateRootType)) {
                string errorMessage = string.Format("The type of '{0}' does not extend interface IAggregateRoot.", aggregateRootType.FullName);
                if(LogManager.Default.IsErrorEnabled)
                    LogManager.Default.Error(errorMessage);
                throw new ThinkNetException(errorMessage);
            }

            IAggregateRoot aggregateRoot;
            if (_cache.TryGet(aggregateRootType, id, out aggregateRoot)) {
                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("find the aggregate root '{0}' of id '{1}' from cache.",
                        aggregateRootType.FullName, id);

                return aggregateRoot;
            }

            var result = Task.Factory.StartNew(delegate {
                using(var context = _dataContextFactory.Create()) {
                    return context.Find(aggregateRootType, id);
                }
            }).Result;

            if(result != null && LogManager.Default.IsDebugEnabled) {
                LogManager.Default.DebugFormat("find the aggregate root '{0}' of id '{1}' from storage.",
                    aggregateRootType.FullName, id);
            }            

            _cache.Set(aggregateRoot, id);

            return result as IAggregateRoot;
        }

        /// <summary>
        /// 保存聚合根。
        /// </summary>
        public void Save(IAggregateRoot aggregateRoot)
        {
            Task.Factory.StartNew(delegate {
                using(var context = _dataContextFactory.Create()) {
                    context.SaveOrUpdate(aggregateRoot);
                    context.Commit();
                }

                _cache.Set(aggregateRoot, aggregateRoot.Id);
            }).Wait();

            if(LogManager.Default.IsDebugEnabled)
                LogManager.Default.DebugFormat("The aggregate root {0} of id {1} is saved.",
                    aggregateRoot.GetType().FullName, aggregateRoot.Id);

            var eventPublisher = aggregateRoot as IEventPublisher;
            if(eventPublisher == null) {
                return;
            }

            _messageBus.PublishAsync(eventPublisher.Events);
        }

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        public void Delete(IAggregateRoot aggregateRoot)
        {
            var aggregateRootType = aggregateRoot.GetType();
            _cache.Remove(aggregateRootType, aggregateRoot.Id);

            Task.Factory.StartNew(delegate {
                using(var context = _dataContextFactory.Create()) {
                    context.Delete(aggregateRoot);
                    context.Commit();
                }
            }).Wait();

            if(LogManager.Default.IsDebugEnabled)
                LogManager.Default.DebugFormat("The aggregate root {0} of id {1} is deleted.",
                    aggregateRootType, aggregateRoot.Id);
        }


        //void IRepository.Delete(Type aggregateRootType, object id)
        //{
        //    CheckType(aggregateRootType);

        //    _cache.Remove(aggregateRootType, id);

        //    if (IsEventSourced(aggregateRootType)) {
        //        _eventSourcedRepository.Delete(aggregateRootType, id);
        //        return;
        //    }

        //    var idType = id.GetType();
        //    var constructor = aggregateRootType.GetConstructor(new[] { idType });
        //    if (constructor == null) {
        //        string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor({1} id)", aggregateRootType.FullName, idType.FullName);
        //        throw new ThinkNetException(errorMessage);
        //    }
        //    var aggregateRoot = (IAggregateRoot)constructor.Invoke(new[] { id });
        //    this.Delete(aggregateRoot);
        //}

        #endregion
    }
}
