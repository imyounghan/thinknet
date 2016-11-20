

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示继承此接口的是一个事件的处理器(主要用于同步数据)。
    /// </summary>
    /// <remarks>
    /// 用于同步数据，将匹配EventStream.Events的个数和类型。
    /// </remarks>
    public interface IEventHandler<TEvent>
        where TEvent : Event
    {
        /// <summary>
        /// 处理事件。
        /// </summary>
        void Handle(SourceMetadata metadata, TEvent @event);
    }

    /// <summary>
    /// 表示继承此接口的是两个事件的处理器。
    /// </summary>
    /// <remarks>
    /// 用于同步数据，将匹配EventStream.Events的个数和类型。
    /// </remarks>
    public interface IEventHandler<TEvent1, TEvent2>
        where TEvent1 : Event
        where TEvent2 : Event
    {
        /// <summary>
        /// 处理事件。
        /// </summary>
        void Handle(SourceMetadata metadata, TEvent1 event1, TEvent2 event2);
    }

    /// <summary>
    /// 表示继承此接口的是三个事件的处理器。
    /// </summary>
    /// <remarks>
    /// 用于同步数据，将匹配EventStream.Events的个数和类型。
    /// </remarks>
    public interface IEventHandler<TEvent1, TEvent2, TEvent3>
        where TEvent1 : Event
        where TEvent2 : Event
        where TEvent3 : Event
    {
        /// <summary>
        /// 处理事件。
        /// </summary>
        void Handle(SourceMetadata metadata, TEvent1 event1, TEvent2 event2, TEvent3 event3);
    }

    /// <summary>
    /// 表示继承此接口的是四个事件的处理器。
    /// </summary>
    /// <remarks>
    /// 用于同步数据，将匹配EventStream.Events的个数和类型。
    /// </remarks>
    public interface IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4>
        where TEvent1 : Event
        where TEvent2 : Event
        where TEvent3 : Event
        where TEvent4 : Event
    {
        /// <summary>
        /// 处理事件。
        /// </summary>
        void Handle(SourceMetadata metadata, TEvent1 event1, TEvent2 event2, TEvent3 event3, TEvent4 event4);
    }

    /// <summary>
    /// 表示继承此接口的是五个事件的处理器。
    /// </summary>
    /// <remarks>
    /// 用于同步数据，将匹配EventStream.Events的个数和类型。
    /// </remarks>
    public interface IEventHandler<TEvent1, TEvent2, TEvent3, TEvent4, TEvent5>
        where TEvent1 : Event
        where TEvent2 : Event
        where TEvent3 : Event
        where TEvent4 : Event
        where TEvent5 : Event
    {
        /// <summary>
        /// 处理事件。
        /// </summary>
        void Handle(SourceMetadata metadata, TEvent1 event1, TEvent2 event2, TEvent3 event3, TEvent4 event4, TEvent5 event5);
    }
}
