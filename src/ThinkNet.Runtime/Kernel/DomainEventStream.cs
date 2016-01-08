using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ThinkNet.Infrastructure;


namespace ThinkNet.Kernel
{
    /// <summary>
    /// 表示领域事件的事件流
    /// </summary>
    [Serializable]
    [HandleOnlyOnce]
    public class DomainEventStream : Infrastructure.Message, Messaging.IEvent
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public DomainEventStream()
            : base(null)
        { }

        /// <summary>
        /// 聚合根标识。
        /// </summary>
        public EventSourcing.SourceKey AggregateRoot { get; set; }
        /// <summary>
        /// 产生事件的命令标识
        /// </summary>
        public string CommandId { get; set; }
        /// <summary>
        /// 事件源
        /// </summary>
        public IEnumerable<Messaging.IEvent> Events { get; set; }

        [IgnoreDataMember]
        string Messaging.IEvent.SourceId
        {
            get { return this.AggregateRoot.SourceId; }
        }

        /// <summary>
        /// 输出领域事件流的字符串格式
        /// </summary>
        public override string ToString()
        {
            return string.Format("[EventId={0},CommandId={1},AggregateRootId={2},AggregateRootType={3},Events={4}]",
                Id,
                CommandId,
                AggregateRoot.SourceId,
                AggregateRoot.SourceTypeName,
                string.Join("|", Events.Select(item => item.GetType().Name)));
        }        
    }
}
