using ThinkNet.EventSourcing;

namespace ThinkNet.Infrastructure
{
    internal class NoneSnapshotStore : ISnapshotStore
    {
        public IEventSourced GetLastest(DataKey sourceKey)
        {
            return null;
        }

        public void Save(IEventSourced snapshot)
        { }

        public void Remove(DataKey sourceKey)
        { }
    }
}
