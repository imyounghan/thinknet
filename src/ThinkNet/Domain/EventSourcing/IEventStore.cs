using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Domain.EventSourcing
{
    /// <summary>
    /// 事件存储。
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// 保存溯源事件。
        /// </summary>
        void Save(EventStream @event);

        ///// <summary>
        ///// 查询该命令下的事件。
        ///// </summary>
        //EventStream Find(DataKey sourceKey, string correlationId);

        /// <summary>
        /// 查询聚合的溯源事件。
        /// </summary>
        IEnumerable<EventStream> FindAll(SourceKey sourceKey, int startVersion);

        /// <summary>
        /// 移除该聚合的溯源事件。
        /// </summary>
        void RemoveAll(SourceKey sourceKey);
    }


    internal class MemoryEventStore : IEventStore
    {

        private readonly ConcurrentDictionary<SourceKey, IList<EventStream>> collection;

        public MemoryEventStore()
        {
            this.collection = new ConcurrentDictionary<SourceKey, IList<EventStream>>();
        }


        public EventStream Find(SourceKey sourceKey, string correlationId)
        {
            IList<EventStream> streams;
            if(!collection.TryGetValue(sourceKey, out streams))
                return null;

            return streams.FirstOrDefault(item => item.CorrelationId == correlationId);
        }


        public IEnumerable<EventStream> FindAll(SourceKey sourceKey, int startVersion)
        {
            IList<EventStream> streams;
            if(!collection.TryGetValue(sourceKey, out streams))
                return Enumerable.Empty<EventStream>();

            return streams.Where(stream => stream.Version > startVersion)
                .OrderBy(stream => stream.Version)
                .ToArray();
        }

        public void RemoveAll(SourceKey sourceKey)
        {
            collection.TryRemove(sourceKey);
        }

        public void Save(EventStream @event)
        {
            var streams = collection.GetOrAdd(@event.SourceId, () => new List<EventStream>());
            if(streams.Count > 0 && streams.Any(p => p.CorrelationId == @event.CorrelationId))
                return;

            streams.Add(@event);
        }
    }
}
