using System;

namespace ThinkNet.Domain
{

    /// <summary>
    /// 表示继承该接口的是一个仓储(不支持多个聚合的事务)。
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// 查找聚合。如果不存在返回null，存在返回实例
        /// </summary>
        IAggregateRoot Find(Type aggregateRootType, object id);

        /// <summary>
        /// 保存聚合根。
        /// </summary>
        void Save(IAggregateRoot aggregateRoot);

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        void Delete(IAggregateRoot aggregateRoot);
    }
}
