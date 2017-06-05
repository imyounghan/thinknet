
namespace ThinkNet.Messaging
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using ThinkNet.Infrastructure;

    public class EventStoreInMemory : IEventStore
    {

        private readonly ConcurrentDictionary<SourceKey, HashSet<EventCollection>> collection;

        public EventStoreInMemory()
        {
            this.collection = new ConcurrentDictionary<SourceKey, HashSet<EventCollection>>();
        }

        public EventCollection Find(SourceKey sourceInfo, string correlationId)
        {
            HashSet<EventCollection> set;
            if (!collection.TryGetValue(sourceInfo, out set)) {
                return null;
            }

            return set.FirstOrDefault(item => item.CorrelationId == correlationId);
        }

        public IEnumerable<EventCollection> FindAll(SourceKey sourceInfo, int startVersion)
        {
            HashSet<EventCollection> set;
            if (!collection.TryGetValue(sourceInfo, out set)) {
                return Enumerable.Empty< EventCollection>();
            }

            return set.Where(item => item.Version > startVersion).ToArray();
        }

        public bool Save(SourceKey sourceInfo, EventCollection eventCollection)
        {
            return collection.GetOrAdd(sourceInfo, () => new HashSet<EventCollection>()).Add(eventCollection);
        }
    }
}
