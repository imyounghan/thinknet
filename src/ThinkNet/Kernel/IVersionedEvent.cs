
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个有序事件。
    /// </summary>
    public interface IVersionedEvent : IEvent
    {
        /// <summary>
        /// 领域事件的来源id
        /// </summary>
        string SourceId { get; }

        /// <summary>
        /// 版本号
        /// </summary>
        int Version { get; }
    }
}
