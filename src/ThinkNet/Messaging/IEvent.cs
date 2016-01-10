
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个事件。
    /// </summary>
    public interface IEvent : IMessage
    {
        /// <summary>
        /// 领域事件的来源id
        /// </summary>
        string SourceId { get; }
    }
}
