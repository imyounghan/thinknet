using System;
using System.Collections.Generic;
using System.Linq;
using ThinkLib.Common;
using ThinkNet.Infrastructure;
using ThinkNet.Kernel;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示创建命令上下文的工厂接口
    /// </summary>
    [UnderlyingComponent(typeof(DefaultCommandContextFactory))]
    public interface ICommandContextFactory
    {
        ICommandContext CreateCommandContext();
    }

    internal class DefaultCommandContextFactory : ICommandContextFactory
    {
        class CommandContext : ICommandContext
        {

            private readonly Func<Type, object, IAggregateRoot> aggregateRootFactory;
            private readonly Action<IAggregateRoot> aggregateRootStore;

            private readonly Dictionary<string, IAggregateRoot> dict;
            private readonly IList<IEvent> pendingEvents;

            public CommandContext(Func<Type, object, IAggregateRoot> factory, Action<IAggregateRoot> store)
            {
                this.aggregateRootFactory = factory;
                this.aggregateRootStore = store;

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

            public void Commit()
            {
                //dict.Values.OfType<IEventSourced>();
                throw new NotImplementedException();
            }

            #endregion
        }

        private readonly IRepository _repository;
        private readonly IEventSourcedRepository _eventSourcedRepository;
        private readonly IEventBus _eventBus;

        public DefaultCommandContextFactory(IRepository repository,
            IEventSourcedRepository eventSourcedRepository, IEventBus eventBus)
        {
            this._repository = repository;
            this._eventSourcedRepository = eventSourcedRepository;
            this._eventBus = eventBus;
        }

        private IAggregateRoot Find(Type type, object id)
        {
            if (type.IsAssignableFrom(typeof(IEventSourced)))
                return _eventSourcedRepository.Find(type, id);
            else
                return null;
        }

        private void Save(IAggregateRoot aggregateRoot)
        {
            
        }

        #region ICommandContextFactory 成员

        public ICommandContext CreateCommandContext()
        {
            return new CommandContext(Find, Save);
        }

        #endregion
    }
}
