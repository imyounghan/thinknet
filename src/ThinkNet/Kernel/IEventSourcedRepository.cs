
namespace ThinkNet.Kernel
{
    /// <summary>
    /// 表示继承该接口的是一个仓储。
    /// </summary>
    public interface IEventSourcedRepository
    {
        /// <summary>
        /// 查找聚合。如果不存在返回null，存在返回实例
        /// </summary>
        TAggregateRoot Find<TAggregateRoot>(object aggregateRootId)
            where TAggregateRoot : class, IEventSourced;

        /// <summary>
        /// 保存聚合根。
        /// </summary>
        void Save<TAggregateRoot>(TAggregateRoot aggregateRoot, string correlationId)
            where TAggregateRoot : class, IEventSourced;

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        void Delete<TAggregateRoot>(TAggregateRoot aggregateRoot)
            where TAggregateRoot : class, IEventSourced;

        ///// <summary>
        ///// 删除聚合根。
        ///// </summary>
        //void Delete(TKey key)
    }
}
