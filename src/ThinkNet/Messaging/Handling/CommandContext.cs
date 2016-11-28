using System;
using System.Collections.Generic;
using System.Linq;
using ThinkLib;
using ThinkNet.Domain;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// <see cref="ICommandContext"/> 的实现类
    /// </summary>
    public class CommandContext : ICommandContext
    {
        private readonly IRepository _repository;
        private readonly IEventSourcedRepository _eventSourcedRepository;
        private readonly IMessageBus _bus;

        private readonly Dictionary<string, IAggregateRoot> dict;
        private readonly IList<Event> pendingEvents;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public CommandContext(IRepository repository, IEventSourcedRepository eventSourcedRepository, IMessageBus bus)
        {
            this._repository = repository;
            this._eventSourcedRepository = eventSourcedRepository;
            this._bus = bus;

            this.dict = new Dictionary<string, IAggregateRoot>();
            this.pendingEvents = new List<Event>();
        }

        private static bool IsEventSourced(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IEventSourced).IsAssignableFrom(type);
        }
        /// <summary>
        /// 添加该聚合根到当前上下文中。
        /// </summary>
        public void Add(IAggregateRoot aggregateRoot)
        {
            aggregateRoot.NotNull("aggregateRoot");

            string key = string.Concat(aggregateRoot.GetType().FullName, "@", aggregateRoot.Id);
            dict.TryAdd(key, aggregateRoot);
        }
        /// <summary>
        /// 从当前上下文获取聚合根
        /// </summary>
        public T Get<T>(object id) where T : class, IAggregateRoot
        {
            var aggregate = this.Find<T>(id);
            if(aggregate == null)
                throw new EntityNotFoundException(id, typeof(T));

            return aggregate as T;
        }

        /// <summary>
        /// 从当前上下文获取聚合根
        /// </summary>
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

        /// <summary>
        /// 添加待处理的事件。
        /// </summary>
        public void AppendEvent(Event @event)
        {
            if(pendingEvents.Any(p => p.Id == @event.Id))
                return;

            pendingEvents.Add(@event);
        }

        private void SendEvents()
        {
            if(pendingEvents.Count > 0)
                _bus.Publish(pendingEvents);
        }
        /// <summary>
        /// 提交修改结果。
        /// </summary>
        public void Commit(string commandId)
        {
            var aggregateRoots = dict.Values.OfType<IEventSourced>().Where(p => !p.Events.IsEmpty());
            var count = aggregateRoots.Count();
            if(count > 1) {
                throw new ThinkNetException("Detected more than one aggregate root created or modified by command.");
            }
            if(count == 1) {
                _eventSourcedRepository.Save(aggregateRoots.First(), commandId);
                SendEvents();
                return;
            }

            //if(dict.Values.Count > 1) {
            //    throw new ThinkNetException("Detected more than one aggregate root created or modified by command.");
            //}
            //if(dict.Values.Count == 1) {
            //    _eventSourcedRepository.Save(aggregateRoots.First(), commandId);
            //    SendEvents();
            //    return;
            //}

            throw new ThinkNetException("No aggregate root found to be created or modified ");
        }
    }
}
