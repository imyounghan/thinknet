using System;
using System.Collections.Generic;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 事件存储。
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// 保存溯源事件。
        /// </summary>
        void Save(string partitionKey, IEnumerable<EventData> events);

        /// <summary>
        /// 判断该命令下是否存在相关事件。
        /// </summary>
        bool EventPersisted(string partitionKey, string correlationId);

        /// <summary>
        /// 查询该命令下的事件。
        /// </summary>
        IEnumerable<EventData> FindAll(string partitionKey, string correlationId);
        /// <summary>
        /// 查询聚合的溯源事件。
        /// </summary>
        IEnumerable<EventData> FindAll(string partitionKey, ComplexKey sourceKey);

        /// <summary>
        /// 移除该聚合的溯源事件。
        /// </summary>
        void RemoveAll(string partitionKey, ComplexKey sourceKey);
    }
}
