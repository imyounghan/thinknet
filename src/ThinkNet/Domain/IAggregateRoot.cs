
namespace ThinkNet.Domain
{
    /// <summary>
    /// 表示继承该接口的类型是一个聚合根
    /// </summary>
    public interface IAggregateRoot
    { 
        /// <summary>
        /// 主键标识
        /// </summary>
        object Id { get; }

        ///// <summary>
        ///// 版本号
        ///// </summary>
        //int Version { get; }

        ///// <summary>
        ///// 通过事件还原对象状态。
        ///// </summary>
        //void LoadFrom(IEnumerable<VersionedEvent> events);
    }
}
