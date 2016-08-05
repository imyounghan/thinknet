
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
        DataStream GetLastest(DataKey sourceKey);
        /// <summary>
        /// 存储聚合快照。
        /// </summary>
        bool Save(DataStream snapshot);
        /// <summary>
        /// 从存储中删除快照。
        /// </summary>
        bool Remove(DataKey sourceKey);
    }    
}
