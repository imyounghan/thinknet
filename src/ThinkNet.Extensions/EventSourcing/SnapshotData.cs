using System;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 快照
    /// </summary>
    public class SnapshotData
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SnapshotData()
        {
            this.CreatedOn = DateTime.UtcNow;
        }
        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public SnapshotData(string sourceId, string sourceType, int version, byte[] payload)
            : this()
        {
            this.SourceKey = new SourceKey(sourceId, sourceType);
            this.Payload = payload;
        }

        /// <summary>
        /// Key。
        /// </summary>
        public SourceKey SourceKey { get; set; }
        /// <summary>
        /// 聚合根数据
        /// </summary>
        public byte[] Payload { get; set; }
        /// <summary>
        /// 创建该快照的时间
        /// </summary>
        public DateTime CreatedOn { get; set; }
    }
}
