using System;

namespace ThinkNet.Domain
{
    /// <summary>
    /// <see cref="IRepository"/> 的扩展类
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// 根据标识id获得聚合实例。如果不存在则会抛异常
        /// </summary>
        public static TAggregateRoot Get<TAggregateRoot>(this IRepository repository, object id)
            where TAggregateRoot : class, IAggregateRoot
        {
            var aggregateRoot = repository.Find<TAggregateRoot>(id);
            if (aggregateRoot == null)
                throw new EntityNotFoundException(id, typeof(TAggregateRoot));

            return aggregateRoot;
        }

        /// <summary>
        /// 根据标识id获得聚合实例。
        /// </summary>
        public static TAggregateRoot Find<TAggregateRoot>(this IRepository repository, object id)
            where TAggregateRoot : class, IAggregateRoot
        {
            return repository.Find(typeof(TAggregateRoot), id) as TAggregateRoot;
        }

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        public static void Delete<TAggregateRoot>(this IRepository repository, object id)
            where TAggregateRoot : class, IAggregateRoot
        {
            repository.Delete(typeof(TAggregateRoot), id);
        }

        /// <summary>
        /// 根据聚合根类型和ID删除
        /// </summary>
        public static void Delete(this IRepository repository, Type aggregateRootType, object id)
        {
            var idType = id.GetType();
            var constructor = aggregateRootType.GetConstructor(new[] { idType });
            if(constructor == null) {
                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor({1} id)", aggregateRootType.FullName, idType.FullName);
                throw new ThinkNetException(errorMessage);
            }
            var aggregateRoot = constructor.Invoke(new[] { id }) as IAggregateRoot;

            if(aggregateRoot == null) {
                string errorMessage = string.Format("The type of '{0}' does not extend interface IAggregateRoot.", aggregateRootType.FullName);
                if(LogManager.Default.IsErrorEnabled)
                    LogManager.Default.Error(errorMessage);
                throw new ThinkNetException(errorMessage);
            }

            repository.Delete(aggregateRoot);
        }

        /// <summary>
        /// 根据标识id获得聚合实例。如果不存在则会抛异常
        /// </summary>
        public static TAggregateRoot Get<TAggregateRoot, TIdentify>(this IRepository<TAggregateRoot> repository, TIdentify id)
            where TAggregateRoot : class, IAggregateRoot
        {
            var aggregateRoot = repository.Find(id);
            if (aggregateRoot == null)
                throw new EntityNotFoundException(id, typeof(TAggregateRoot));

            return aggregateRoot;
        }

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        public static void Delete<TAggregateRoot>(this IEventSourcedRepository repository, object id)
            where TAggregateRoot : class, IEventSourced
        {
            repository.Delete(typeof(TAggregateRoot), id);
        }

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        public static void Delete(this IEventSourcedRepository repository, IEventSourced eventSourced)
        {
            repository.Delete(eventSourced.GetType(), eventSourced.Id);
        }

        /// <summary>
        /// 从当前仓储中移除聚合根
        /// </summary>
        public static void Remove<TAggregateRoot, TIdentify>(this IRepository<TAggregateRoot> repository, TIdentify id)
            where TAggregateRoot : class, IAggregateRoot
        {
            var aggregateRoot = repository.Find(id);
            if (aggregateRoot != null)
                repository.Remove(aggregateRoot);
        }
    }
}
