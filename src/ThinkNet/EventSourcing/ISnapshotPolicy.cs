using ThinkLib.Common;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 表示生成聚合快照策略的接口
    /// </summary>
    [UnderlyingComponent(typeof(NoneSnapshotPolicy))]
    public interface ISnapshotPolicy
    {
        /// <summary>
        /// 创建快照
        /// </summary>
        bool ShouldbeCreateSnapshot(Stream snapshot);
    }


    internal class NoneSnapshotPolicy : ISnapshotPolicy
    {
        public bool ShouldbeCreateSnapshot(Stream snapshot)
        {
            return false;
        }
        //private readonly ConcurrentDictionary<int, int> _snapshotVersion;
        ///// <summary>
        ///// Parameterized Constructor.
        ///// </summary>
        //public DefaultSnapshotPolicy()
        //{
        //    this._snapshotVersion = new ConcurrentDictionary<int, int>();
        //}

        ///// <summary>
        ///// 获取触发保存快照的间隔版本号。
        ///// </summary>
        //private int GetTriggeredVersion(Type aggregateRootType)
        //{
        //    int aggregateRootTypeCode = aggregateRootType.AssemblyQualifiedName.GetHashCode();

        //    return _snapshotVersion.GetOrAdd(aggregateRootTypeCode, key => {
        //        var attribute = aggregateRootType.GetAttribute<SnapshotPolicyAttribute>(false);

        //        if (attribute == null) {
        //            return 50;
        //        }

        //        return attribute.TriggeredVersion;
        //    });
        //}


        //public bool ShouldbeCreateSnapshot(IEventSourced eventSourced)
        //{
        //    if (eventSourced == null || eventSourced.Version <= 0)
        //        return false;

        //    var aggregateRootType = eventSourced.GetType();
        //    var triggered = GetTriggeredVersion(aggregateRootType);

        //    return eventSourced.Version % triggered == 0;
        //}        
    }
}
