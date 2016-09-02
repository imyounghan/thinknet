using System.Collections.Generic;
using System.Linq;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示这是由聚合发出来的领域事件
    /// </summary>
    public sealed class EventStream : Message, IEvent
    {
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
        /// 起始版本号
        /// </summary>
        public int Version { get; set; }
        ///// <summary>
        /// 事件源
        /// </summary>
        public IEnumerable<IEvent> Events { get; set; }

        /// <summary>
        /// 输出领域事件流的字符串格式
        /// </summary>
        public override string ToString()
        {
            var events = this.Events.Select(@event => string.Concat(@event.GetType().FullName, "&", @event.Id));
            return string.Concat(this.SourceNamespace, ".", this.SourceTypeName, "@", this.SourceId,
                "[", string.Join(";", events), "]", "#", this.CommandId);
        }
    }
}
