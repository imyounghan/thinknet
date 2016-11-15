using ThinkNet.Domain.EventSourcing;

namespace ThinkNet.Messaging.Handling
{
    public class VersionData
    {
        public VersionData(DataKey key, int version)
        {
            this.Key = key;
            this.Version = version;
        }

        public DataKey Key { get; private set; }

        public int Version { get; private set; }
    }
}
