using ThinkNet.Domain.EventSourcing;

namespace ThinkNet.Messaging.Handling
{
    public class VersionData
    {
        public DataKey Key { get; set; }

        public int Version { get; set; }
    }
}
