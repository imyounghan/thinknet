using System.Collections.Generic;
using ThinkNet.EventSourcing;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 事件存储。
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// 保存溯源事件。如果该命令产生的事件已保存过则为false，否则为true
        /// </summary>
        void Save(VersionedEvent @event);

        /// <summary>
        /// 查询该命令下的事件。
        /// </summary>
        VersionedEvent Find(DataKey sourceKey, string correlationId);

        /// <summary>
        /// 查询聚合的溯源事件。
        /// </summary>
        IEnumerable<VersionedEvent> FindAll(DataKey sourceKey, int startVersion);

        /// <summary>
        /// 移除该聚合的溯源事件。
        /// </summary>
        void RemoveAll(DataKey sourceKey);
    }    
}
