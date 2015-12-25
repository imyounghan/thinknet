using ThinkNet.Infrastructure;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// 表示继承该接口的类型是一个聚合根
    /// </summary>
    public interface IAggregateRoot : IEntity
    {
        /// <summary>
        /// 主键标识
        /// </summary>
        object Id { get; }
    }
}
