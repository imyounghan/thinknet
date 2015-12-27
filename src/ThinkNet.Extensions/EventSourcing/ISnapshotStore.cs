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
        SnapshotData GetLastest(SourceKey sourceKey);
        /// <summary>
        /// 存储聚合快照。
        /// </summary>
        void Save(SnapshotData snapshot);
        /// <summary>
        /// 从存储中删除快照。
        /// </summary>
        void Remove(SourceKey sourceKey);
    }
}
