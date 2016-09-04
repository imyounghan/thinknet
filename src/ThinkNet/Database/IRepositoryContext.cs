using System;

namespace ThinkNet.Database
{
    /// <summary>
    /// 仓储上下文
    /// </summary>
    public interface IRepositoryContext : IDisposable
    {
        /// <summary>
        /// 获取仓储
        /// </summary>
        IRepository<TAggregateRoot> GetRepository<TAggregateRoot>()
            where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        /// 获取仓储
        /// </summary>
        TRepository GetRepository<TRepository, TAggregateRoot>()
            where TRepository : class, IRepository<TAggregateRoot>
            where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        /// 提交事务
        /// </summary>
        void Commit();
    }
}
