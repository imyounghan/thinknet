using System;
using System.Collections.Generic;
using System.IO;
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

        public EventStream(object sourceId, Type sourceType)
            : this(sourceId.ToString(),
            sourceType.Namespace,
            sourceType.Name,
            Path.GetFileNameWithoutExtension(sourceType.Assembly.ManifestModule.FullyQualifiedName))
        { }

        public EventStream(string sourceId, string sourceNamespace, string sourceTypeName)
            : this(sourceId, sourceNamespace, sourceTypeName, string.Empty)
        { }

        public EventStream(string sourceId, string sourceNamespace, string sourceTypeName, string sourceAssemblyName)
        {
            this.SourceId = sourceId;
            this.SourceNamespace = sourceNamespace;
            this.SourceTypeName = sourceTypeName;
            this.SourceAssemblyName = sourceAssemblyName;
        }

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
