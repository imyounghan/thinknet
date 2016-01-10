
namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示继承此接口的是一个事件处理器。
    /// </summary>
    public interface IEventHandler<in TEvent> : IHandler
        where TEvent : class, IEvent
    {
        /// <summary>
        /// 处理事件。
        /// </summary>
        void Handle(TEvent @event);
    }
}
