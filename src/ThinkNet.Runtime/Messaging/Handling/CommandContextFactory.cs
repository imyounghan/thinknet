using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Kernel;

namespace ThinkNet.Messaging.Handling
{
    public class CommandContextFactory : ICommandContextFactory
    {
        public class CommandContext : ICommandContext
        {

            private readonly Func<Type, object, IAggregateRoot> aggregateRootFactory;
            private readonly Action<IAggregateRoot, string> aggregateRootStore;

            private readonly Action<IEnumerable<IEvent>> eventPublisher;

            private readonly Dictionary<string, IAggregateRoot> dict;
            private readonly IList<IEvent> pendingEvents;

            public CommandContext(Func<Type, object, IAggregateRoot> factory,
                Action<IAggregateRoot, string> store,
                Action<IEnumerable<IEvent>> publisher)
            {
                this.aggregateRootFactory = factory;
                this.aggregateRootStore = store;
                this.eventPublisher = publisher;

                this.dict = new Dictionary<string, IAggregateRoot>();
                this.pendingEvents = new List<IEvent>();
            }

            #region ICommandContext 成员

            public void Add(IAggregateRoot aggregateRoot)
            {
                string key = string.Concat(aggregateRoot.GetType().Name, "@", aggregateRoot.Id);
                if (!dict.ContainsKey(key)) {
                    dict.Add(key, aggregateRoot);
                }
            }

            public T Get<T>(object id) where T : class, IAggregateRoot
            {
                var aggregate = this.Find<T>(id);
                if (aggregate == null)
                    throw new EntityNotFoundException(id, typeof(T));

                return aggregate as T;
            }

            public T Find<T>(object id) where T : class, IAggregateRoot
            {
                var type = typeof(T);
                string key = string.Concat(type.Name, "@", id);

                IAggregateRoot aggregateRoot;
                if (!dict.TryGetValue(key, out aggregateRoot)) {
                    aggregateRoot = aggregateRootFactory(type, id);
                    dict.Add(key, aggregateRoot);
                }

                return aggregateRoot as T;
            }

            public void PendingEvent(IEvent @event)
            {
                if (pendingEvents.Any(p => p.Id == @event.Id))
                    return;

                pendingEvents.Add(@event);
            }

            #endregion

            #region IUnitOfWork 成员

            public void Commit(string commandId)
            {
                if (dict.Count == 0)
                    return;

                var sourceds = dict.Values.OfType<IEventSourced>();
                if (!Commit(dict.Values.OfType<IEventSourced>(), commandId)) {
                    Commit(dict.Values.Where(p => !(p is IEventSourced)));
                }


                if (pendingEvents.Count > 0)
                    eventPublisher(pendingEvents);
            }

            private bool Commit(IEnumerable<IEventSourced> aggregateRoots, string commandId)
            {
                if (aggregateRoots.IsEmpty())
                    return false;

                aggregateRoots = aggregateRoots.Where(p => !p.GetEvents().IsEmpty());

                switch (aggregateRoots.Count()) {
                    case 0:
                        return false;
                    case 1:
                        break;
                    default:
                        throw new ThinkNetException("Detected more than one aggregate created or modified by command.");
                }

                aggregateRootStore(aggregateRoots.First(), commandId);

                return true;
            }

            private bool Commit(IEnumerable<IAggregateRoot> aggregateRoots)
            {
                if (aggregateRoots.IsEmpty())
                    return false;

                switch (aggregateRoots.Count()) {
                    case 0:
                        return false;
                    case 1:
                        break;
                    default:
                        throw new ThinkNetException("Detected more than one aggregate created or modified by command.");
                }

                aggregateRootStore(aggregateRoots.First(), null);

                return true;
            }

            #endregion
        }

        private readonly IRepository _repository;
        private readonly IEventSourcedRepository _eventSourcedRepository;
        private readonly IEventBus _eventBus;

        public CommandContextFactory(IRepository repository,
            IEventSourcedRepository eventSourcedRepository, IEventBus eventBus)
        {
            this._repository = repository;
            this._eventSourcedRepository = eventSourcedRepository;
            this._eventBus = eventBus;
        }

        private void PublishEvents(IEnumerable<IEvent> events)
        {
            this._eventBus.Publish(events);
        }

        private IAggregateRoot Find(Type type, object id)
        {
            if (type.IsAssignableFrom(typeof(IEventSourced)))
                return _eventSourcedRepository.Find(type, id);
            else
                return _repository.Find(type, id);
        }

        private void Save(IAggregateRoot aggregateRoot, string commandId)
        {
            var eventSourced = aggregateRoot as IEventSourced;
            if (eventSourced.IsNull())
                _repository.Save(aggregateRoot);
            else
                _eventSourcedRepository.Save(eventSourced, commandId);
        }

        #region ICommandContextFactory 成员

        public ICommandContext CreateCommandContext()
        {
            return new CommandContext(Find, Save, PublishEvents);
        }

        #endregion
    }
}
