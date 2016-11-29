using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThinkLib;
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

        /// <summary>
        /// 查询该命令下的事件。
        /// </summary>
        EventStream Find(DataKey sourceKey, string correlationId);

        /// <summary>
        /// 查询聚合的溯源事件。
        /// </summary>
        IEnumerable<EventStream> FindAll(DataKey sourceKey, int startVersion);

        /// <summary>
        /// 移除该聚合的溯源事件。
        /// </summary>
        void RemoveAll(DataKey sourceKey);
    }

    internal class MemoryEventStore : IEventStore
    {
        /// <summary>
        /// 表示事件或快照的数据流
        /// </summary>
        class Stream
        {
            public string CorrelationId { get; set; }

            public int Version { get; set; }

            public IEnumerable<Event> Events { get; set; }
        }

        private readonly ConcurrentDictionary<DataKey, IList<Stream>> collection;

        public MemoryEventStore()
        {
            this.collection = new ConcurrentDictionary<DataKey, IList<Stream>>();
        }


        public EventStream Find(DataKey sourceKey, string correlationId)
        {
            IList<Stream> streams;
            if(!collection.TryGetValue(sourceKey, out streams))
                return null;

            var stream = streams.FirstOrDefault(item => item.CorrelationId == correlationId);
            if(stream == null)
                return null;

            return new EventStream() {
                CorrelationId = stream.CorrelationId,
                Events = stream.Events,
                SourceId = sourceKey,
                Version = stream.Version
            };
        }
        

        public IEnumerable<EventStream> FindAll(DataKey sourceKey, int startVersion)
        {
            IList<Stream> streams;
            if(!collection.TryGetValue(sourceKey, out streams))
                return Enumerable.Empty<EventStream>();

            return streams.Where(stream => stream.Version > startVersion)
                .Select(stream => new EventStream() {
                    CorrelationId = stream.CorrelationId,
                    Events = stream.Events,
                    SourceId = sourceKey,
                    Version = stream.Version
                })
                .OrderBy(stream => stream.Version)
                .ToArray();
        }

        public void RemoveAll(DataKey sourceKey)
        {
            collection.TryRemove(sourceKey);
        }

        public void Save(EventStream @event)
        {
            var streams = collection.GetOrAdd(@event.SourceId, () => new List<Stream>());
            if(streams.Count > 0 && streams.Any(p => p.CorrelationId == @event.CorrelationId))
                return;

            streams.Add(new Stream() {
                CorrelationId = @event.CorrelationId,
                Version = @event.Version,
                Events = @event.Events
            });
        }
    }
}
