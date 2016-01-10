using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// 事件流
    /// </summary>
    [Serializable]
    [JustHandleOnce]
    public class EventStream : Event
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public EventStream()
        { }

        ///// <summary>
        ///// 聚合根标识。
        ///// </summary>
        //public EventSourcing.SourceKey AggregateRoot { get; set; }
        /// <summary>
        /// 程序集
        /// </summary>
        public string SourceAssemblyName { get; set; }
        /// <summary>
        /// 命名空间
        /// </summary>
        public string SourceNamespace { get; set; }
        /// <summary>
        /// 类型名称
        /// </summary>
        public string SourceTypeName { get; set; }
        /// <summary>
        /// 标识。
        /// </summary>
        public string SourceId { get; set; }
        /// <summary>
        /// 产生事件的命令标识
        /// </summary>
        public string CommandId { get; set; }
        /// <summary>
        /// 事件源
        /// </summary>
        public IEnumerable<IEvent> Events { get; set; }

        protected override string GetSourceStringId()
        {
            return this.SourceId;
        }


        /// <summary>
        /// 输出领域事件流的字符串格式
        /// </summary>
        public override string ToString()
        {
            string events = string.Join("|", Events.Select(item => item.ToString()));

            return string.Format("EventId={0},CommandId={1},AggregateRootId={2},AggregateRootType={3}.{4},Events={5}",
                Id,
                CommandId,
                SourceId,
                SourceNamespace,
                SourceTypeName,
                events);
        }
    }
}
