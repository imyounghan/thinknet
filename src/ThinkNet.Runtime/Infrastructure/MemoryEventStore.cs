using System.Collections.Generic;
using System.Linq;
using ThinkNet.EventSourcing;

namespace ThinkNet.Infrastructure
{
    internal class MemoryEventStore : IEventStore
    {

        private readonly HashSet<VersionedEvent> events;

        public MemoryEventStore()
        {
            this.events = new HashSet<VersionedEvent>();
        }

        #region IEventStore 成员

        public void Save(VersionedEvent @event)
        {
            events.Add(@event);
        }

        public VersionedEvent Find(DataKey sourceKey, string correlationId)
        {
            correlationId.NotNullOrWhiteSpace("correlationId");

            return events.FirstOrDefault(p => new DataKey(p.SourceId, p.SourceType) == sourceKey && p.CommandId == correlationId);
        }


        public IEnumerable<VersionedEvent> FindAll(DataKey sourceKey, int version)
        {
            return events.Where(p => new DataKey(p.SourceId, p.SourceType) == sourceKey && p.Version > version).OrderBy(p => p.Version).ToArray();
        }

        public void RemoveAll(DataKey sourceKey)
        {
            events.RemoveWhere(p => new DataKey(p.SourceId, p.SourceType) == sourceKey);
        }

        #endregion
    }
}
