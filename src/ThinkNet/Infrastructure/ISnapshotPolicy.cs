using ThinkNet.EventSourcing;

namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 表示生成聚合快照策略的接口
    /// </summary>
    public interface ISnapshotPolicy
    {
        /// <summary>
        /// 创建快照
        /// </summary>
        bool ShouldbeCreateSnapshot(IEventSourced snapshot);
    }    
}
