using System;
using System.Collections.Generic;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 存储快照
    /// </summary>
    public interface ISnapshotStore
    {
        /// <summary>
        /// 获取最新的快照。
        /// </summary>
        Tuple<int, byte[]> GetLastest(SourceKey sourceKey);
        /// <summary>
        /// 存储聚合快照。
        /// </summary>
        void Save(SourceKey sourceKey, int version, byte[] snapshot);
        /// <summary>
        /// 从存储中删除快照。
        /// </summary>
        void Remove(SourceKey sourceKey);
    }
}
