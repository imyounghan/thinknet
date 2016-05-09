using System.Collections.Generic;
using System.Linq;
using ThinkNet.Messaging;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkLib.Serialization;

namespace ThinkNet.Kernel
{

    /// <summary>
    /// 表示继承该接口的是一个仓储。
    /// </summary>
    [UnderlyingComponent(typeof(MemoryRepository))]
    public interface IRepository
    {
        /// <summary>
        /// 查找聚合。如果不存在返回null，存在返回实例
        /// </summary>
        TAggregateRoot Find<TAggregateRoot, TKey>(TKey key)
            where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        /// 保存聚合根。
        /// </summary>
        void Save<TAggregateRoot>(TAggregateRoot aggregateRoot, string correlationId) 
            where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        void Delete<TAggregateRoot>(TAggregateRoot aggregateRoot) 
            where TAggregateRoot : class, IAggregateRoot;

        /// <summary>
        /// 删除聚合根。
        /// </summary>
        void Delete<TAggregateRoot, TKey>(TKey key)
            where TAggregateRoot : class, IAggregateRoot;
    }

    internal class MemoryRepository : IRepository
    {
        private readonly Dictionary<int, ISet<IAggregateRoot>> dictionary;
        private readonly IEventBus eventBus;
        private readonly ITextSerializer serializer;

        public MemoryRepository(IEventBus eventBus, ITextSerializer serializer)
        {
            this.dictionary = new Dictionary<int, ISet<IAggregateRoot>>();
            this.eventBus = eventBus;
            this.serializer = serializer;
        }


        public TAggregateRoot Find<TAggregateRoot, TKey>(TKey key) where TAggregateRoot : class, IAggregateRoot
        {
            var typeCode = typeof(TAggregateRoot).FullName.GetHashCode();
            ISet<IAggregateRoot> set;

            if (!dictionary.TryGetValue(typeCode, out set)) {
                return null;
            }

            return set.Where(p => p.Id.Equals(key)).FirstOrDefault() as TAggregateRoot;
        }

        public void Save<TAggregateRoot>(TAggregateRoot aggregateRoot, string correlationId) where TAggregateRoot : class, IAggregateRoot
        {
            var type = typeof(TAggregateRoot);
            var typeCode = type.FullName.GetHashCode();
            var set = dictionary.GetOrAdd(typeCode, code => new HashSet<IAggregateRoot>());
            if (!set.Add(aggregateRoot))
                return;

            var eventPublisher = aggregateRoot as IEventPublisher;
            if (eventPublisher == null)
                return;

            var events = eventPublisher.Events;
            if (string.IsNullOrWhiteSpace(correlationId)) {
                eventBus.Publish(events);
            }
            else {
                eventBus.Publish(new EventStream(aggregateRoot.Id, type) {
                    CommandId = correlationId,
                    Events = events.Select(item => new EventStream.Stream(item.GetType()) {
                        Payload = serializer.Serialize(item)
                    }).ToArray()
                });
            }
            LogManager.GetLogger("ThinkZoo").InfoFormat("publish all events. events: [{0}]", string.Join("|",
                events.Select(@event => @event.ToString())));
        }

        public void Delete<TAggregateRoot>(TAggregateRoot aggregateRoot) where TAggregateRoot : class, IAggregateRoot
        {
            if (aggregateRoot == null)
                return;

            var typeCode = typeof(TAggregateRoot).FullName.GetHashCode();
            ISet<IAggregateRoot> set;

            if (!dictionary.TryGetValue(typeCode, out set)) {
                return;
            }

            set.Remove(aggregateRoot);
        }

        public void Delete<TAggregateRoot, TKey>(TKey key) where TAggregateRoot : class, IAggregateRoot
        {
            this.Delete(this.Find<TAggregateRoot, TKey>(key));
        }

        public IQueryable<TAggregateRoot> Query<TAggregateRoot>() where TAggregateRoot : class, IAggregateRoot
        {
            var typeCode = typeof(TAggregateRoot).FullName.GetHashCode();
            ISet<IAggregateRoot> set;

            if (!dictionary.TryGetValue(typeCode, out set)) {
                return Enumerable.Empty<TAggregateRoot>().AsQueryable();
            }

            return set.OfType<TAggregateRoot>().AsQueryable();
        }
    }
}
