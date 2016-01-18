using System;
using ThinkNet.Infrastructure;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// 表示事件溯源时的异常
    /// </summary>
    public class EventSourcedException : ThinkNetException
    {
        private const string DifferentAggregateRoot = "Cannot apply event to aggregate root as the AggregateRootId not matched. DomainEvent SourceId:{0}; Current AggregateRootId:{1}";
        private const string DifferentAggregateRootVersion = "Cannot apply event to aggregate root as the version not matched. DomainEvent Version:{0}; Current AggregateRoot Version:{1}";


        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventSourcedException(string eventSourceId, string aggregateRootId) :
            base(string.Format(DifferentAggregateRoot, eventSourceId, aggregateRootId)) 
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventSourcedException(int eventVersion, int aggregateRootVersion) :
            base(string.Format(DifferentAggregateRootVersion, eventVersion, aggregateRootVersion))
        { }        

        /// <summary>
        /// default constructor.
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
    }
}
