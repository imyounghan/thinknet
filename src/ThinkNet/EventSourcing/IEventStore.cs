using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Configurations;

namespace ThinkNet.EventSourcing
{
    [UnderlyingComponent(typeof(MemoryEventStore))]
    /// <summary>
    /// 事件存储。
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// 保存溯源事件。如果该命令产生的事件已保存过则为false，否则为true
        /// </summary>
        bool Save(SourceKey sourceKey, string correlationId, Func<IEnumerable<Stream>> eventsFactory);

        ///// <summary>
        ///// 判断该命令下是否存在相关事件。
        ///// </summary>
        //bool EventPersisted(SourceKey sourceKey, string correlationId);

        /// <summary>
        /// 查询该命令下的事件。
        /// </summary>
        IEnumerable<Stream> FindAll(SourceKey sourceKey, string correlationId);
        /// <summary>
        /// 查询聚合的溯源事件。
        /// </summary>
        IEnumerable<Stream> FindAll(SourceKey sourceKey, int version);

        /// <summary>
        /// 移除该聚合的溯源事件。
        /// </summary>
        void RemoveAll(SourceKey sourceKey);
    }


    internal class MemoryEventStore : IEventStore
    {
        private readonly ConcurrentDictionary<SourceKey, IDictionary<Stream, string>> collection;

        public MemoryEventStore()
        {
            this.collection = new ConcurrentDictionary<SourceKey, IDictionary<Stream, string>>();
        }

        #region IEventStore 成员

        public bool Save(SourceKey sourceKey, string correlationId, Func<IEnumerable<Stream>> eventsFactory)
        {
            if (!EventPersisted(sourceKey, correlationId)) {
                var streams = collection.GetOrAdd(sourceKey, key => new Dictionary<Stream, string>());
                eventsFactory().ForEach(item => streams.Add(item, correlationId));
                return true;
            }

            return false;
        }

        public bool EventPersisted(SourceKey sourceKey, string correlationId)
        {
            return !this.FindAll(sourceKey, correlationId).IsEmpty();
        }

        public IEnumerable<Stream> FindAll(SourceKey sourceKey, string correlationId)
        {
            IDictionary<Stream, string> streams;
            if (!collection.TryGetValue(sourceKey, out streams) )
                return Enumerable.Empty<Stream>();

            return streams.Where(item => item.Value == correlationId).Select(item => item.Key).AsEnumerable();
        }

        public IEnumerable<Stream> FindAll(SourceKey sourceKey, int version)
        {
            IDictionary<Stream, string> streams;
            if (!collection.TryGetValue(sourceKey, out streams))
                return Enumerable.Empty<Stream>();

            return streams.Keys.Where(item => item.Version > version).AsEnumerable();
        }

        public void RemoveAll(SourceKey sourceKey)
        {
            collection.Remove(sourceKey);
        }

        #endregion
    }
}
