using ThinkNet.Infrastructure;

namespace ThinkNet.Domain.EventSourcing
{
    /// <summary>
    /// 存储快照
    /// </summary>
    public interface ISnapshotStore
    {
        /// <summary>
        /// 获取最新的快照。
        /// </summary>
        T GetLastest<T>(SourceKey sourceKey) where T : class;
        /// <summary>
        /// 存储聚合快照。
        /// </summary>
        void Save(IAggregateRoot aggregateRoot);
        /// <summary>
        /// 从存储中删除快照。
        /// </summary>
        void Remove(SourceKey sourceKey);
    }

    internal class NoneSnapshotStore : ISnapshotStore
    {
        public void Remove(SourceKey sourceKey)
        { }

        public void Save(IAggregateRoot aggregateRoot)
        { }

        T ISnapshotStore.GetLastest<T>(SourceKey sourceKey)
        {
            return null;
        }
    }
}
