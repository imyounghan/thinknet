using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Database;
using ThinkNet.Messaging;

namespace ThinkNet.Infrastructure
{
    internal class MemoryRepository : IRepository
    {
        private readonly Dictionary<int, ISet<IAggregateRoot>> dictionary;
        private readonly IEventBus eventBus;

        public MemoryRepository(IEventBus eventBus)
        {
            this.dictionary = new Dictionary<int, ISet<IAggregateRoot>>();
            this.eventBus = eventBus;
        }


        public IAggregateRoot Find(Type aggregateRootType, object id)
        {
            if (!aggregateRootType.IsAssignableFrom(typeof(IAggregateRoot))) {
                string errorMessage = string.Format("The type of '{0}' does not extend interface IAggregateRoot.", aggregateRootType.FullName);
                if (LogManager.Default.IsErrorEnabled)
                    LogManager.Default.Error(errorMessage);
                throw new AggregateRootException(errorMessage);
            }

            var typeCode = aggregateRootType.FullName.GetHashCode();
            ISet<IAggregateRoot> set;

            if (!dictionary.TryGetValue(typeCode, out set)) {
                return null;
            }

            return set.Where(p => p.Id.GetHashCode() == id.GetHashCode()).FirstOrDefault();
        }

        public void Save(IAggregateRoot aggregateRoot)
        {
            var type = aggregateRoot.GetType();
            var typeCode = type.FullName.GetHashCode();
            var set = dictionary.GetOrAdd(typeCode, () => new HashSet<IAggregateRoot>());
            if (!set.Add(aggregateRoot))
                return;

            var eventPublisher = aggregateRoot as IEventPublisher;
            if (eventPublisher == null)
                return;

            var events = eventPublisher.Events;

            if (events.IsEmpty())
                return;

            eventBus.Publish(events);

            if (LogManager.Default.IsDebugEnabled)
                LogManager.Default.DebugFormat("publish all events. events: [{0}]", string.Join("|", events));
        }

        public void Delete(IAggregateRoot aggregateRoot)
        {
            if (aggregateRoot == null)
                return;

            var typeCode = aggregateRoot.GetType().FullName.GetHashCode();
            ISet<IAggregateRoot> set;

            if (!dictionary.TryGetValue(typeCode, out set)) {
                return;
            }

            set.Remove(aggregateRoot);
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
