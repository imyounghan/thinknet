using System;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 表示事件溯源的聚合根异常
    /// </summary>
    [Serializable]
    public class EventSourcedException : ThinkNetException
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public EventSourcedException()
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventSourcedException(string message)
            : base(message)
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventSourcedException(string message, Exception innerException)
            : base(message, innerException)
        { }


        private const string DifferentAggregateRoot = "Cannot load because the event sourceid '{0}' is not equal to the entity id '{1}'.";
        private const string DifferentAggregateRootVersion = "Cannot load because the eventstream version '{0}' is not equal to the entity version '{1}'.";
        private const string CannotFindInnerEventHandler = "Event handler not found on {0} for {1}.";

        /// <summary>
        /// 由于未找到聚合内部的EventHandler引发的异常。
        /// </summary>
        public EventSourcedException(Type eventType, Type aggregateRootType) :
            base(string.Format(CannotFindInnerEventHandler, aggregateRootType.FullName, eventType.FullName))
        { }
        /// <summary>
        /// 由于事件的聚合根ID和要溯源的聚合根ID不一致引发的异常。
        /// </summary>
        public EventSourcedException(string eventSourceId, string aggregateRootId) :
            base(string.Format(DifferentAggregateRoot, eventSourceId, aggregateRootId)) 
        { }
        /// <summary>
        /// 由于事件的版本号和要溯源的聚合根版本号不一致引发的异常。
        /// </summary>
        public EventSourcedException(int eventVersion, int aggregateRootVersion) :
            base(string.Format(DifferentAggregateRootVersion, eventVersion, aggregateRootVersion))
        { }
    }
}
