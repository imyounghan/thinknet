using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.EventSourcing;

namespace ThinkNet.Messaging.Handling
{
    public class CommandContextFactory : ICommandContextFactory
    {
        class CommandContext : ICommandContext
        {
            private readonly Func<Type, object, IEventSourced> aggregateRootFactory;
            private readonly Action<IEventSourced, string> aggregateRootStore;

            private readonly Action<IEnumerable<IEvent>> eventPublisher;

            private readonly Dictionary<string, IEventSourced> dict;
            private readonly IList<IEvent> pendingEvents;

            public CommandContext(Func<Type, object, IEventSourced> factory,
                Action<IEventSourced, string> store,
                Action<IEnumerable<IEvent>> publisher)
            {
                this.aggregateRootFactory = factory;
                this.aggregateRootStore = store;
                this.eventPublisher = publisher;

                this.dict = new Dictionary<string, IEventSourced>();
                this.pendingEvents = new List<IEvent>();
            }

            public void Add(IEventSourced eventSourced)
            {
                string key = string.Concat(eventSourced.GetType().FullName, "@", eventSourced.Id);
                if (!dict.ContainsKey(key)) {
                    dict.Add(key, eventSourced);
                }
            }

            public T Get<T>(object id) where T : class, IEventSourced
            {
                var aggregate = this.Find<T>(id);
                if (aggregate == null)
                    throw new EntityNotFoundException(id, typeof(T));

                return aggregate as T;
            }

            public T Find<T>(object id) where T : class, IEventSourced
            {
                var type = typeof(T);
                string key = string.Concat(type.Name, "@", id);

                IEventSourced aggregateRoot;
                if (!dict.TryGetValue(key, out aggregateRoot)) {
                    aggregateRoot = aggregateRootFactory.Invoke(type, id);
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

            public void Commit(string commandId)
            {

                switch (dict.Count) {
                    case 0:
                        break;
                    case 1:
                        aggregateRootStore(dict.Values.First(), commandId);                        
                        break;
                    default:
                        throw new ThinkNetException("Detected more than one aggregate created or modified by command.");
                }

                if (pendingEvents.Count > 0)
                    eventPublisher.Invoke(pendingEvents);
            }
        }

        private readonly IEventSourcedRepository _eventSourcedRepository;
        private readonly IEventBus _eventBus;

        public CommandContextFactory(IEventSourcedRepository eventSourcedRepository, IEventBus eventBus)
        {
            this._eventSourcedRepository = eventSourcedRepository;
            this._eventBus = eventBus;
        }

        private void PublishEvents(IEnumerable<IEvent> events)
        {
            _eventBus.Publish(events);
        }

        private IEventSourced FindEventSourced(Type type, object id)
        {
            return _eventSourcedRepository.Find(type, id);
        }

        private void SaveEventSourced(IEventSourced eventSourced, string commandId)
        {
            _eventSourcedRepository.Save(eventSourced, commandId);
        }

        #region ICommandContextFactory 成员

        public ICommandContext CreateCommandContext()
        {
            return new CommandContext(FindEventSourced, SaveEventSourced, PublishEvents);
        }

        #endregion
    }
}
