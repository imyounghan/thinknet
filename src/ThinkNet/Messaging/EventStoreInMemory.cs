
namespace ThinkNet.Messaging
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    public class EventStoreInMemory : IEventStore
    {

        private readonly ConcurrentDictionary<SourceInfo, HashSet<EventCollection>> collection;

        public EventStoreInMemory()
        {
            this.collection = new ConcurrentDictionary<SourceInfo, HashSet<EventCollection>>();
        }

        public EventCollection Find(SourceInfo sourceInfo, string correlationId)
        {
            HashSet<EventCollection> set;
            if (!collection.TryGetValue(sourceInfo, out set)) {
                return null;
            }

            return set.FirstOrDefault(item => item.CorrelationId == correlationId);
        }

        public IEnumerable<EventCollection> FindAll(SourceInfo sourceInfo, int startVersion)
        {
            HashSet<EventCollection> set;
            if (!collection.TryGetValue(sourceInfo, out set)) {
                return Enumerable.Empty< EventCollection>();
            }

            return set.Where(item => item.Version > startVersion).ToArray();
        }

        public bool Save(SourceInfo sourceInfo, EventCollection eventCollection)
        {
            return collection.GetOrAdd(sourceInfo, () => new HashSet<EventCollection>()).Add(eventCollection);
        }
    }
}
