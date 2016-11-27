using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ThinkNet.Domain.EventSourcing;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示这是一个可溯源的有序事件流。
    /// </summary>
    [DataContract]
    [Serializable]
    public sealed class EventStream : IMessage, IUniquelyIdentifiable
    {
        /// <summary>
        /// 源ID
        /// </summary>
        [DataMember(Name = "sourceId")]
        public DataKey SourceId { get; set; }

        /// <summary>
        /// 产生事件的相关标识(如命令的id)
        /// </summary>
        [DataMember(Name = "correlationId")]
        public string CorrelationId { get; set; }
        /// <summary>
        /// 版本号
        /// </summary>
        [DataMember(Name = "version")]
        public int Version { get; set; }

        /// <summary>
        /// 事件源
        /// </summary>
        [DataMember(Name = "events")]
        public IEnumerable<Event> Events { get; set; }


        /// <summary>
        /// 确定此实例是否与指定的对象相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as EventStream;
            if (other == null) {
                return false;
            }

            return other.SourceId == this.SourceId && other.Version == this.Version;
        }

        /// <summary>
        /// 返回此实例的哈希代码。
        /// </summary>
        public override int GetHashCode()
        {
            return SourceId.GetHashCode() ^ Version.GetHashCode();
        }

        /// <summary>
        /// 输出领域事件流的字符串格式
        /// </summary>
        public override string ToString()
        {
            var events = this.Events.Select(@event => string.Concat(@event.GetType().FullName, "&", @event.Id));

            return string.Concat(this.SourceId.GetSourceTypeName(), "@", this.SourceId.Id,
                "[", string.Join(",", events), "]", "#", this.CorrelationId);
        }
        

        string IMessage.GetKey()
        {
            return this.SourceId.Id;
        }

        [IgnoreDataMember]
        string IUniquelyIdentifiable.Id
        {
            get
            {
                return this.CorrelationId;
            }
        }
    }
}
