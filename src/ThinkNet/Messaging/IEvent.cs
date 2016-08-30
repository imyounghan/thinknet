using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个事件。
    /// </summary>
    public interface IEvent : IMessage
    {
        /// <summary>
        /// 获取事件ID
        /// </summary>
        string SourceId { get; set; }
    }
}
