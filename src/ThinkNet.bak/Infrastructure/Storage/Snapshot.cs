using System;
using System.Linq;

namespace ThinkNet.Infrastructure.Storage
{
    /// <summary>
    /// 聚合快照
    /// </summary>
    public class Snapshot
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Snapshot()
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public Snapshot(Type aggregateRootType, string aggregateRootId)
        {
            this.AggregateRootId = aggregateRootId;
            this.AggregateRootTypeCode = aggregateRootType.FullName.GetHashCode();
            this.AggregateRootTypeName = aggregateRootType.GetFullName();
            this.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// 聚合根标识
        /// </summary>
        public string AggregateRootId { get; set; }
        /// <summary>
        /// 聚合根类型名称
        /// </summary>
        public int AggregateRootTypeCode { get; set; }
        /// <summary>
        /// 聚合根类型名称
        /// </summary>
        public string AggregateRootTypeName { get; set; }
        /// <summary>
        /// 创建该聚合快照的聚合根版本号
        /// </summary>
        public int Version { get; set; }        
        /// <summary>
        /// 聚合根数据
        /// </summary>
        public byte[] Data { get; set; }
        /// <summary>
        /// 创建该快照的时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 返回此实例的哈希代码
        /// </summary>
        public override int GetHashCode()
        {
            return new int[] {
                AggregateRootTypeCode.GetHashCode(),
                AggregateRootId.GetHashCode(),
                Version
            }.Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// 确定此实例是否与指定的对象（也必须是 <see cref="Snapshot"/> 对象）相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as Snapshot;
            if (ReferenceEquals(null, other)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }

            return other.AggregateRootTypeCode == this.AggregateRootTypeCode
                && other.AggregateRootId == this.AggregateRootId
                && other.Version == this.Version;
        }

        /// <summary>
        /// 将此实例的标识转换为其等效的字符串表示形式。
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}@{1}:{2}", AggregateRootTypeName, AggregateRootId, Version);
        }
    }
}
