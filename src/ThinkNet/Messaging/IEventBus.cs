
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示领域事件总线的接口。
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 发现领域事件
        /// </summary>
        /// <param name="sourceInfo">事件来源</param>
        /// <param name="eventCollection">领域事件的集合</param>
        /// <param name="command">产生事件的命令</param>
        void Publish(SourceInfo sourceInfo, EventCollection eventCollection, Envelope<ICommand> command);
    }
}
