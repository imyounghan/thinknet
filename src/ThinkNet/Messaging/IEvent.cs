

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个事件。
    /// </summary>
    public interface IEvent : IMessage
    {
        /// <summary>
        /// 事件的唯一标识
        /// </summary>
        string Id { get; }
        /// <summary>
        /// 生成事件的创建时间
        /// </summary>
        System.DateTime CreationTime { get; }
    }
}
