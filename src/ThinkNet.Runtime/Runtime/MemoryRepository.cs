using System;
using System.Collections.Generic;
using System.Linq;
using ThinkLib.Logging;
using ThinkNet.Kernel;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    internal class MemoryRepository : IRepository
    {
        private readonly Dictionary<int, ISet<IAggregateRoot>> dictionary;
        private readonly IEventBus eventBus;
        private readonly ILogger _logger;

        public MemoryRepository(IEventBus eventBus)
        {
            this.dictionary = new Dictionary<int, ISet<IAggregateRoot>>();
            this.eventBus = eventBus;
            this._logger = LogManager.GetLogger("ThinkNet");
        }


        public IAggregateRoot Find(Type aggregateRootType, object id)
        {
            if (!aggregateRootType.IsAssignableFrom(typeof(IAggregateRoot))) {
                string errorMessage = string.Format("The type of '{0}' does not extend interface IAggregateRoot.", aggregateRootType.FullName);
                if (_logger.IsErrorEnabled)
                    _logger.Error(errorMessage);
                throw new EventSourcedException(errorMessage);
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
            var set = dictionary.GetOrAdd(typeCode, code => new HashSet<IAggregateRoot>());
            if (!set.Add(aggregateRoot))
                return;

            var eventPublisher = aggregateRoot as IEventPublisher;
            if (eventPublisher == null)
                return;

            var events = eventPublisher.Events;

            if (events.IsEmpty())
                return;

            eventBus.Publish(events);
            _logger.InfoFormat("publish all events. events: [{0}]", string.Join("|",
                events.Select(@event => @event.ToString())));
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
