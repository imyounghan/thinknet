
namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示继承此接口的是一个事件处理器。
    /// </summary>
    public interface IEventHandler<TEvent> : IHandler
        where TEvent : class, IEvent
    {
        /// <summary>
        /// 处理事件。
        /// </summary>
        void Handle(IEventContext context, TEvent @event);
    }

    //public interface IEventHandler<TEvent1, TEvent2> : IHandler
    //    where TEvent1 : class, IEvent
    //    where TEvent2 : class, IEvent
    //{
    //    /// <summary>
    //    /// 处理事件。
    //    /// </summary>
    //    void Handle(TEvent1 event1, TEvent2 event2);
    //}

    //public interface IEventHandler<TEvent1, TEvent2, TEvent3> : IHandler
    //    where TEvent1 : class, IEvent
    //    where TEvent2 : class, IEvent
    //    where TEvent3 : class, IEvent
    //{
    //    /// <summary>
    //    /// 处理事件。
    //    /// </summary>
    //    void Handle(TEvent1 event1, TEvent2 event2, TEvent3 event3);
    //}

    //public interface IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4> : IHandler
    //    where TEvent1 : class, IEvent
    //    where TEvent2 : class, IEvent
    //    where TEvent3 : class, IEvent
    //    where TEvent4 : class, IEvent
    //{
    //    /// <summary>
    //    /// 处理事件。
    //    /// </summary>
    //    void Handle(TEvent1 event1, TEvent2 event2, TEvent3 event3, TEvent4 event4);
    //}

    //public interface IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5> : IHandler
    //    where TEvent1 : class, IEvent
    //    where TEvent2 : class, IEvent
    //    where TEvent3 : class, IEvent
    //    where TEvent4 : class, IEvent
    //    where TEvent5 : class, IEvent
    //{
    //    /// <summary>
    //    /// 处理事件。
    //    /// </summary>
    //    void Handle(TEvent1 event1, TEvent2 event2, TEvent3 event3, TEvent4 event4, TEvent5 event5);
    //}
}
