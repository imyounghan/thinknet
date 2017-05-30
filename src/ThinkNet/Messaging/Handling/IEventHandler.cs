
namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示继承该接口的是溯源事件处理程序
    /// </summary>
    public interface IEventHandler : IHandler
    { }

    /// <summary>
    /// 表示继承该接口的是一个溯源事件处理程序
    /// </summary>
    public interface IEventHandler<TEvent> : IEventHandler
        where TEvent : Event
    {
        /// <summary>
        /// 处理事件。
        /// </summary>
        void Handle(IEventContext context, TEvent @event);
    }

    /// <summary>
    /// 表示继承此接口的是两个溯源事件的处理器。
    /// </summary>
    public interface IEventHandler<TEvent1, TEvent2> : IEventHandler
        where TEvent1 : Event
        where TEvent2 : Event
    {
        /// <summary>
        /// 处理事件。
        /// </summary>
        void Handle(IEventContext context, TEvent1 event1, TEvent2 event2);
    }

    /// <summary>
    /// 表示继承此接口的是三个溯源事件的处理器。
    /// </summary>
    public interface IEventHandler<TEvent1, TEvent2, TEvent3> : IEventHandler
        where TEvent1 : Event
        where TEvent2 : Event
        where TEvent3 : Event
    {
        /// <summary>
        /// 处理事件。
        /// </summary>
        void Handle(IEventContext context, TEvent1 event1, TEvent2 event2, TEvent3 event3);
    }
}
