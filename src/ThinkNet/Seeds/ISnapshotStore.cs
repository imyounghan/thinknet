

namespace ThinkNet.Seeds
{
    using System;

    /// <summary>
    /// 存储快照
    /// </summary>
    public interface ISnapshotStore
    {
        /// <summary>
        /// 获取最新的快照。
        /// </summary>
        IAggregateRoot GetLastest(Type sourceTypeName, object sourceId);
    }
}
