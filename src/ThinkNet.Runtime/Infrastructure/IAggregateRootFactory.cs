using System;
using ThinkNet.Kernel;


namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示创建聚合根的接口
    /// </summary>
    public interface IAggregateRootFactory
    {
        /// <summary>
        /// 根据类型和标识创建一个聚合根
        /// </summary>
        IAggregateRoot Create(Type type, object id);

        /// <summary>
        /// 
        /// </summary>
        T Create<T>(object id) where T : class, IAggregateRoot;
    }
}
