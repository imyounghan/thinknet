using System;
using ThinkNet.Domain.EventSourcing;

namespace ThinkNet.Domain.Repositories
{
    public abstract class RepositoryBase : IRepository
    {

        private readonly IEventSourcedRepository _eventSourcedRepository;
        private readonly ICache _cache;

        protected RepositoryBase(IEventSourcedRepository eventSourcedRepository, ICache cache)
        {
            this._eventSourcedRepository = eventSourcedRepository;
            this._cache = cache;
        }

        #region IRepository 成员

        public abstract IAggregateRoot Find(Type aggregateRootType, object id);

        public abstract void Save(IAggregateRoot aggregateRoot);

        public abstract void Delete(IAggregateRoot aggregateRoot);


        #endregion

        private static bool IsEventSourced(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IEventSourced).IsAssignableFrom(type);
        }

        private static bool IsAggregateRoot(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IAggregateRoot).IsAssignableFrom(type);
        }

        private static void CheckType(Type type)
        {
            if (!IsAggregateRoot(type)) {
                string errorMessage = string.Format("The type of '{0}' does not extend interface IAggregateRoot.", type.FullName);
                if (LogManager.Default.IsErrorEnabled)
                    LogManager.Default.Error(errorMessage);
                throw new ThinkNetException(errorMessage);
            }
        }

        #region IRepository 成员

        IAggregateRoot IRepository.Find(Type aggregateRootType, object id)
        {
            CheckType(aggregateRootType);

            var aggregateRoot = _cache.Get(aggregateRootType, id) as IAggregateRoot;
            if (aggregateRoot != null) {
                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("find the aggregate root '{0}' of id '{1}' from cache.",
                        aggregateRootType.FullName, id);

                return aggregateRoot;
            }

            if (IsEventSourced(aggregateRootType)) {
                aggregateRoot = _eventSourcedRepository.Find(aggregateRootType, id);
            }
            else {
                aggregateRoot = this.Find(aggregateRootType, id);
            }

            _cache.Set(aggregateRoot, id);

            return aggregateRoot;
        }

        void IRepository.Save(IAggregateRoot aggregateRoot, string correlationId)
        {
            IEventSourced eventSourced = aggregateRoot as IEventSourced;
            if (eventSourced != null) {
                _eventSourcedRepository.Save(eventSourced, correlationId);
            }
            else {
                this.Save(aggregateRoot);
            }

            _cache.Set(aggregateRoot, aggregateRoot.Id);
        }

        void IRepository.Delete(IAggregateRoot aggregateRoot)
        {
            var aggregateRootType = aggregateRoot.GetType();
            _cache.Remove(aggregateRootType, aggregateRoot.Id);

            if (aggregateRoot is IEventSourced) {
                _eventSourcedRepository.Delete(aggregateRootType, aggregateRoot.Id);
            }
            else {
                this.Delete(aggregateRoot);
            }
        }


        void IRepository.Delete(Type aggregateRootType, object id)
        {
            CheckType(aggregateRootType);

            _cache.Remove(aggregateRootType, id);

            if (IsEventSourced(aggregateRootType)) {
                _eventSourcedRepository.Delete(aggregateRootType, id);
                return;
            }

            var idType = id.GetType();
            var constructor = aggregateRootType.GetConstructor(new[] { idType });
            if (constructor == null) {
                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor({1} id)", aggregateRootType.FullName, idType.FullName);
                throw new ThinkNetException(errorMessage);
            }
            var aggregateRoot = (IAggregateRoot)constructor.Invoke(new[] { id });
            this.Delete(aggregateRoot);
        }

        #endregion
    }
}
