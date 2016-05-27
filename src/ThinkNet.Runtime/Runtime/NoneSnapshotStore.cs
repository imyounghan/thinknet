using ThinkNet.EventSourcing;

namespace ThinkNet.Runtime
{
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
