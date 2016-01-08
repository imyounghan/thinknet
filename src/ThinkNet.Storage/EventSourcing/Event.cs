﻿using System;
using System.Linq;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 历史事件(用于还原溯源聚合的事件)
    /// </summary>
    [Serializable]
    public class Event
    {
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
        /// 事件流
        /// </summary>
        public string Payload { get; set; }
        /// <summary>
        /// 发布事件的相关id
        /// </summary>
        public string CorrelationId { get; set; }
        /// <summary>
        /// 生成事件的时间戳
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
                Version.GetHashCode()
            }.Aggregate((x, y) => x ^ y);
        }

        /// <summary>
        /// 确定此实例是否与指定的对象（也必须是 <see cref="Snapshot"/> 对象）相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as Event;
            if (other == null) {
                return false;
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
            return string.Format("{0}_{1}_{2}", AggregateRootTypeCode, AggregateRootId, Version);
        }
    }
}
