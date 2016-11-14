using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThinkNet.Messaging;

namespace ThinkNet.Domain.EventSourcing
{
    /// <summary>
    /// 表示这是一个可溯源的有序事件流。
    /// </summary>
    public sealed class EventStream : IMessage
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public EventStream()
        { }

        /// <summary>
        /// 产生事件的相关标识(如命令的id)
        /// </summary>
        public string CorrelationId { get; set; }
        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// 事件源
        /// </summary>
        public IEnumerable<IEvent> Events { get; set; }
        /// <summary>
        /// 事件源的标识id(如聚合根ID)
        /// </summary>
        public string SourceId { get; set; }
        /// <summary>
        /// 事件源的类型(如聚合根类型)
        /// </summary>
        public Type SourceType { get; set; }


        /// <summary>
        /// 确定此实例是否与指定的对象相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as EventStream;
            if (other == null) {
                return false;
            }

            return other.SourceType == this.SourceType && other.Version == this.Version;
        }

        /// <summary>
        /// 返回此实例的哈希代码。
        /// </summary>
        public override int GetHashCode()
        {
            var codes = new int[] {
                Path.GetFileNameWithoutExtension(this.SourceType.Assembly.ManifestModule.FullyQualifiedName).GetHashCode(),
                this.SourceType.FullName.GetHashCode(),
                this.SourceId.GetHashCode(),
                this.Version.GetHashCode()
            };
            return codes.Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// 输出领域事件流的字符串格式
        /// </summary>
        public override string ToString()
        {
            var events = this.Events.Select(@event => string.Concat(@event.GetType().FullName, "&", @event.Id));

            return string.Concat(this.SourceType.FullName, "@", this.SourceId,
                "[", string.Join(",", events), "]", "#", this.CorrelationId);
        }

        #region IMessage 成员

        string IMessage.GetKey()
        {
            return this.SourceId;
        }

        #endregion
    }
}
