using System;
using ThinkNet.Infrastructure;

namespace ThinkNet.Database
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
            var aggregateRoot = (IAggregateRoot)Activator.CreateInstance(typeof(TAggregateRoot), new[] { id });

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
