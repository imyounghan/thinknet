using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Messaging;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 表示这是一个有序事件。
    /// </summary>
    public sealed class VersionedEvent : Message, IEvent
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public VersionedEvent()
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public VersionedEvent(string id, DateTime time)
            : base(id, time)
        { }

        /// <summary>
        /// 产生事件的命令标识
        /// </summary>
        public string CommandId { get; set; }
        /// <summary>
        /// 起始版本号
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// 事件源
        /// </summary>
        public IEnumerable<IEvent> Events { get; set; }
        /// <summary>
        /// 事件源的标识
        /// </summary>
        public string SourceId { get; set; }
        /// <summary>
        /// 事件源的类型
        /// </summary>
        public Type SourceType { get; set; }

        ///// <summary>
        ///// 确定此实例是否与指定的对象相同。
        ///// </summary>
        //public override bool Equals(object obj)
        //{
        //    var other = obj as VersionedEvent;
        //    if(other == null) {
        //        return false;
        //    }

        //    return other.SourceType == this.SourceType && other.Version == this.Version;
        //}

        ///// <summary>
        ///// 返回此实例的哈希代码。
        ///// </summary>
        //public override int GetHashCode()
        //{
        //    var codes = new int[] {
        //        Path.GetFileNameWithoutExtension(this.SourceType.Assembly.ManifestModule.FullyQualifiedName).GetHashCode(),
        //        this.SourceType.FullName.GetHashCode(),
        //        this.SourceId.GetHashCode(),
        //        this.Version
        //    };
        //    return codes.Aggregate((x, y) => x ^ y);
        //}

        /// <summary>
        /// 输出领域事件流的字符串格式
        /// </summary>
        public override string ToString()
        {
            var events = this.Events.Select(@event => string.Concat(@event.GetType().FullName, "&", @event.Id));

            return string.Concat(this.SourceType.FullName, "@", this.SourceId,
                "[", string.Join(",", events), "]", "#", this.CommandId);
        }
    }
}
