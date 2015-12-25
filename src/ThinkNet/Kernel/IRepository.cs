
namespace ThinkNet.Kernel
{
    /// <summary>
    /// 表示继承该接口的是一个仓储。
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// 查找聚合。如果不存在返回null，存在返回实例
        /// </summary>
        TAggregateRoot Find<TAggregateRoot, TKey>(TKey key)
            where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        /// 保存聚合根。
        /// </summary>
        void Save<TAggregateRoot>(TAggregateRoot aggregateRoot, string correlationId) 
            where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        void Delete<TAggregateRoot>(TAggregateRoot aggregateRoot) 
            where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        void Delete<TAggregateRoot, TKey>(TKey key)
            where TAggregateRoot : class, IAggregateRoot;
    }

    /// <summary>
    /// 表示继承该接口的是一个聚合仓储。
    /// </summary>
    public interface IRepository<TAggregateRoot>
        where TAggregateRoot : class, IAggregateRoot
    {
        /// <summary>
        /// 添加聚合到仓储
        /// </summary>
        void Add(TAggregateRoot aggregateRoot);

        /// <summary>
        /// 从仓储中移除聚合
        /// </summary>
        void Remove(TAggregateRoot aggregateRoot);

        /// <summary>
        /// 查找聚合。如果不存在返回null，存在返回实例
        /// </summary>
        TAggregateRoot Find<TIdentify>(TIdentify id);
    }
}
