
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的类型是一个命令。
    /// </summary>
    public interface ICommand : IMessage
    {
        /// <summary>
        /// 获取聚合根ID
        /// </summary>
        string AggregateRootId { get; }
    }
}
