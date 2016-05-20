
namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示继承此接口的是一个事件处理器。
    /// </summary>
    public interface IEventHandler<TEvent> : IHandler
        where TEvent : class, IVersionedEvent
    {
        /// <summary>
        /// 处理事件。
        /// </summary>
        void Handle(IEventContext context, TEvent @event);
    }
}
