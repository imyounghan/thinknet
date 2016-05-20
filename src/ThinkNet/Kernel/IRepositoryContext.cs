using System;
using ThinkLib.Common;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// 仓储上下文
    /// </summary>
    public interface IRepositoryContext : IUnitOfWork, IDisposable
    {
        /// <summary>
        /// 获取仓储
        /// </summary>
        TRepository GetRepository<TRepository>()
            where TRepository : class, IRepository<IAggregateRoot>;
    }
}
