using System.Collections.Generic;
using ThinkNet.Messaging;

namespace ThinkNet.Domain.EventSourcing
{
    /// <summary>
    /// 事件存储。
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// 保存溯源事件。
        /// </summary>
        void Save(EventStream @event);

        /// <summary>
        /// 查询该命令下的事件。
        /// </summary>
        EventStream Find(DataKey sourceKey, string correlationId);

        /// <summary>
        /// 查询聚合的溯源事件。
        /// </summary>
        IEnumerable<EventStream> FindAll(DataKey sourceKey, int startVersion);

        /// <summary>
        /// 移除该聚合的溯源事件。
        /// </summary>
        void RemoveAll(DataKey sourceKey);
    }    
}
