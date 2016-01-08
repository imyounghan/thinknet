using System;
using System.Linq;

namespace ThinkNet.Kernel
{
    /// <summary>
    /// 表示具有版本号的事件流
    /// </summary>
    [Serializable]
    public class VersionedEventStream : DomainEventStream
    {
        /// <summary>
        /// 起始版本号
        /// </summary>
        public int StartVersion { get; set; }
        /// <summary>
        /// 结束版本号
        /// </summary>
        public int EndVersion { get; set; }

        /// <summary>
        /// 输出带有版本号事件流的字符串格式
        /// </summary>
        public override string ToString()
        {
            return string.Format("[EventId={0},CommandId={1},AggregateRootType={2},AggregateRootId={3},Version={4}~{5},Events={6}]",
                Id,
                CommandId,
                AggregateRoot.SourceTypeName,
                AggregateRoot.SourceId,
                StartVersion,
                EndVersion,
                string.Join("|", Events.Select(item => item.GetType().Name)));
        }
    }
}
