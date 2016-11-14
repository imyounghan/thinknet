using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Domain;

namespace ThinkNet.Messaging.Handling
{
    public class CommandContext : ICommandContext
    {
        private readonly IRepository _repository;
        private readonly IEventSourcedRepository _eventSourcedRepository;
        private readonly IMessageBus _bus;

        private readonly Dictionary<string, IAggregateRoot> dict;
        private readonly IList<IEvent> pendingEvents;

        public CommandContext(IRepository repository, IEventSourcedRepository eventSourcedRepository, IMessageBus bus)
        {
            this._repository = repository;
            this._eventSourcedRepository = eventSourcedRepository;
            this._bus = bus;

            this.dict = new Dictionary<string, IAggregateRoot>();
            this.pendingEvents = new List<IEvent>();
        }

        private static bool IsEventSourced(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IEventSourced).IsAssignableFrom(type);
        }

        public void Add(IAggregateRoot aggregateRoot)
        {
            aggregateRoot.NotNull("aggregateRoot");

            string key = string.Concat(aggregateRoot.GetType().FullName, "@", aggregateRoot.Id);
            dict.TryAdd(key, aggregateRoot);
        }

        public T Get<T>(object id) where T : class, IAggregateRoot
        {
            var aggregate = this.Find<T>(id);
            if(aggregate == null)
                throw new EntityNotFoundException(id, typeof(T));

            return aggregate as T;
        }

        public T Find<T>(object id) where T : class, IAggregateRoot
        {
            var type = typeof(T);
            string key = string.Concat(type.FullName, "@", id);

            IAggregateRoot aggregateRoot;
            if(!dict.TryGetValue(key, out aggregateRoot)) {
                if (IsEventSourced(type)) {
                    aggregateRoot = _eventSourcedRepository.Find(type, id);
                }
                else {
                    aggregateRoot = _repository.Find(type, id);
                }

                if (aggregateRoot != null) {
                    dict.Add(key, aggregateRoot);
                }
            }

            return aggregateRoot as T;
        }

        public void PendingEvent(IEvent @event)
        {
            if(pendingEvents.Any(p => p.Id == @event.Id))
                return;

            pendingEvents.Add(@event);
        }

        public void Commit(string commandId)
        {

            //switch (dict.Count) {
            //    case 0:
            //        break;
            //    case 1:
            //        aggregateRootStore(dict.Values.First(), commandId);
            //        break;
            //    default:
            //        throw new ThinkNetException("Detected more than one aggregate created or modified by command.");
            //}

            if(pendingEvents.Count > 0)
                _bus.Publish(pendingEvents);
        }
    }
}
