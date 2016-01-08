using System;
using System.Linq;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 表示已发布的事件版本数据
    /// </summary>
    [Serializable]
    public class EventPublishedVersion
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public EventPublishedVersion()
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventPublishedVersion(int aggregateRootTypeCode, string aggregateRootId, int version)
        {
            this.AggregateRootId = aggregateRootId;
            this.AggregateRootTypeCode = aggregateRootTypeCode;
            this.Version = version;
        }

        /// <summary>
        /// 聚合根标识。
        /// </summary>
        public string AggregateRootId { get; set; }
        /// <summary>
        /// 聚合根类型编码。
        /// </summary>
        public int AggregateRootTypeCode { get; set; }
        /// <summary>
        /// 版本号。
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 返回此实例的哈希代码
        /// </summary>
        public override int GetHashCode()
        {
            return new int[] {
                AggregateRootTypeCode.GetHashCode(),
                AggregateRootId.GetHashCode(),
                Version.GetHashCode()
            }.Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// 确定此实例是否与指定的对象（也必须是 <see cref="EventPublishedVersion"/> 对象）相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as EventPublishedVersion;
            if (other == null) {
                return false;
            }

            return other.AggregateRootTypeCode == this.AggregateRootTypeCode &&
                other.AggregateRootId == this.AggregateRootId &&
                other.Version == this.Version;
        }

        /// <summary>
        /// 将此实例的标识转换为其等效的字符串表示形式。
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}_{1}_{2}", AggregateRootTypeCode, AggregateRootId, Version);
        }
    }
}
