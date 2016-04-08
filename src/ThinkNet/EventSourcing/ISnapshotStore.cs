using ThinkLib.Common;

namespace ThinkNet.EventSourcing
{
    [UnderlyingComponent(typeof(NoneSnapshotStore))]
    /// <summary>
    /// 存储快照
    /// </summary>
    public interface ISnapshotStore
    {
        /// <summary>
        /// 获取最新的快照。
        /// </summary>
        Stream GetLastest(SourceKey sourceKey);
        /// <summary>
        /// 存储聚合快照。
        /// </summary>
        bool Save(Stream snapshot);
        /// <summary>
        /// 从存储中删除快照。
        /// </summary>
        bool Remove(SourceKey sourceKey);
    }


    internal class NoneSnapshotStore : ISnapshotStore
    {
        public Stream GetLastest(SourceKey sourceKey)
        {
            return null;
        }

        public bool Save(Stream snapshot)
        {
            return false;
        }

        public bool Remove(SourceKey sourceKey)
        {
            return false;
        }
    }
}
