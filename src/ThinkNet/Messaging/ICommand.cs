using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个命令。
    /// </summary>
    [JustHandleOnce]
    public interface ICommand : IMessage
    {
        /// <summary>
        /// 获取聚合根标识
        /// </summary>
        string AggregateRootId { get; }
    }
}
