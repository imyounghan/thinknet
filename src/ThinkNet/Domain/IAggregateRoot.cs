using ThinkNet.Infrastructure;

namespace ThinkNet.Domain
{
    /// <summary>
    /// 表示继承该接口的类型是一个聚合根
    /// </summary>
    public interface IAggregateRoot : IUniquelyIdentifiable
    { }
}
