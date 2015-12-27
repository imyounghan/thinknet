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
        void Save(SourceKey sourceKey, IEnumerable<EventData> events);

        /// <summary>
        /// 判断该命令下是否存在相关事件。
        /// </summary>
        bool EventPersisted(SourceKey sourceKey, string correlationId);

        /// <summary>
        /// 查询该命令下的事件。
        /// </summary>
        IEnumerable<EventData> FindAll(SourceKey sourceKey, string correlationId);
        /// <summary>
        /// 查询聚合的溯源事件。
        /// </summary>
        IEnumerable<EventData> FindAll(SourceKey sourceKey);

        /// <summary>
        /// 移除该聚合的溯源事件。
        /// </summary>
        void RemoveAll(SourceKey sourceKey);
    }
}
