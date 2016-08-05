using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ThinkNet.Infrastructure
{
    internal class MemoryEventStore : IEventStore
    {
        private readonly ConcurrentDictionary<DataKey, IDictionary<DataStream, string>> collection;

        public MemoryEventStore()
        {
            this.collection = new ConcurrentDictionary<DataKey, IDictionary<DataStream, string>>();
        }

        #region IEventStore 成员

        public bool Save(DataKey sourceKey, string correlationId, IEnumerable<DataStream> events)
        {
            if (!EventPersisted(sourceKey, correlationId)) {
                var streams = collection.GetOrAdd(sourceKey, key => new Dictionary<DataStream, string>());
                events.ForEach(item => streams.Add(item, correlationId));
                return true;
            }

            return false;
        }

        public bool EventPersisted(DataKey sourceKey, string correlationId)
        {
            return !this.FindAll(sourceKey, correlationId).IsEmpty();
        }

        public IEnumerable<DataStream> FindAll(DataKey sourceKey, string correlationId)
        {
            IDictionary<DataStream, string> streams;
            if (!collection.TryGetValue(sourceKey, out streams))
                return Enumerable.Empty<DataStream>();

            return streams.Where(item => item.Value == correlationId).Select(item => item.Key).AsEnumerable();
        }

        public IEnumerable<DataStream> FindAll(DataKey sourceKey, int version)
        {
            IDictionary<DataStream, string> streams;
            if (!collection.TryGetValue(sourceKey, out streams))
                return Enumerable.Empty<DataStream>();

            return streams.Keys.Where(item => item.Version > version).AsEnumerable();
        }

        public void RemoveAll(DataKey sourceKey)
        {
            collection.Remove(sourceKey);
        }

        #endregion
    }
}
