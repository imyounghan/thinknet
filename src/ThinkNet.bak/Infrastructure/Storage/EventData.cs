using System;
using System.Collections.Generic;
using System.Linq;

namespace ThinkNet.Infrastructure.Storage
{
    /// <summary>
    /// 历史事件(用于还原溯源聚合的事件)
    /// </summary>
    public class EventData
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public EventData()
        { }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public EventData(Type aggregateRootType, string aggregateRootId)
        {
            this.AggregateRootId = aggregateRootId;
            this.AggregateRootTypeCode = aggregateRootType.FullName.GetHashCode();
            this.AggregateRootTypeName = aggregateRootType.GetFullName();
            this.Timestamp = DateTime.UtcNow;
            this.Items = new List<EventDataItem>();
        }
        

        /// <summary>
        /// 用于数据库的自增主键
        /// </summary>
        public long EventId { get; set; }
        /// <summary>
        /// 聚合根标识。
        /// </summary>
        public string AggregateRootId { get; set; }
        /// <summary>
        /// 聚合根类型的完整名称且包括程序集名称
        /// </summary>
        public string AggregateRootTypeName { get; set; }
        /// <summary>
        /// 聚合根类型编码。
        /// </summary>
        public int AggregateRootTypeCode { get; set; }
        /// <summary>
        /// 版本号。
        /// </summary>
        public int Version { get; set; }        
        /// <summary>
        /// 发布事件的相关id
        /// </summary>
        public string CorrelationId { get; set; }
        /// <summary>
        /// 生成事件的时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 事件项
        /// </summary>
        public ICollection<EventDataItem> Items { get; set; }
        /// <summary>
        /// 添加一个事件
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(EventDataItem item)
        {
            item.Order = Items.Count + 1;
            this.Items.Add(item);
        }

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
        /// 确定此实例是否与指定的对象（也必须是 <see cref="Snapshot"/> 对象）相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as EventData;
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
