using ThinkNet.Infrastructure;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// <see cref="IRepository{TAggregateRoot}"/> 的扩展类
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// 根据标识id获得聚合实例。如果不存在则会抛异常
        /// </summary>
        public static TAggregateRoot Get<TAggregateRoot, TIdentify>(this IRepository<TAggregateRoot> repository, TIdentify id)
            where TAggregateRoot : class, IAggregateRoot
        {
            var aggregate = repository.Find(id);
            if (aggregate == null)
                throw new EntityNotFoundException(id, typeof(TAggregateRoot));

            return aggregate;
        }

        /// <summary>
        /// 根据标识id获得聚合实例。如果不存在则会抛异常
        /// </summary>
        public static TAggregateRoot Get<TAggregateRoot, TIdentify>(this IRepository repository, TIdentify id)
            where TAggregateRoot : class, IAggregateRoot
        {
            var aggregate = repository.Find<TAggregateRoot, TIdentify>(id);
            if (aggregate == null)
                throw new EntityNotFoundException(id, typeof(TAggregateRoot));

            return aggregate;
        }
    }
}
