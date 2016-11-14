
namespace ThinkNet.Domain.EventSourcing
{
    internal class NoneSnapshotPolicy : ISnapshotPolicy
    {
        public bool ShouldbeCreateSnapshot(IEventSourced snapshot)
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
