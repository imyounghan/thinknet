using ThinkNet.EventSourcing;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 存储快照
    /// </summary>
    public interface ISnapshotStore
    {
        /// <summary>
        /// 获取最新的快照。
        /// </summary>
        IEventSourced GetLastest(DataKey sourceKey);
        /// <summary>
        /// 存储聚合快照。
        /// </summary>
        void Save(IEventSourced snapshot);
        /// <summary>
        /// 从存储中删除快照。
        /// </summary>
        void Remove(DataKey sourceKey);
    }    
}
