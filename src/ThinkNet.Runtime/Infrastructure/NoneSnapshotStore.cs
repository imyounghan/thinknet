
namespace ThinkNet.Infrastructure
{
    internal class NoneSnapshotStore : ISnapshotStore
    {
        public DataStream GetLastest(DataKey sourceKey)
        {
            return null;
        }

        public bool Save(DataStream snapshot)
        {
            return false;
        }

        public bool Remove(DataKey sourceKey)
        {
            return false;
        }
    }
}
