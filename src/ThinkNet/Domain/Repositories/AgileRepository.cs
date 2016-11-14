using System;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Database;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Messaging;

namespace ThinkNet.Domain.Repositories
{
    public sealed class AgileRepository : RepositoryBase
    {
        private readonly IDataContextFactory _dataContextFactory;
        private readonly IMessageBus _messageBus;

        public AgileRepository(IDataContextFactory dataContextFactory, IMessageBus messageBus,
            IEventSourcedRepository eventSourcedRepository, ICache cache)
            : base(eventSourcedRepository, cache)
        {
            this._dataContextFactory = dataContextFactory;
            this._messageBus = messageBus;
        }

        public override IAggregateRoot Find(Type aggregateRootType, object id)
        {
            var result = Task.Factory.StartNew(() => {
                using (var context = _dataContextFactory.Create()) {
                    return context.Find(aggregateRootType, id);
                }
            }).Result;

            if (result != null && LogManager.Default.IsDebugEnabled) {
                LogManager.Default.DebugFormat("find the aggregate root '{0}' of id '{1}' from storage.",
                    aggregateRootType.FullName, id);
            }

            return result as IAggregateRoot;
        }

        public override void Save(IAggregateRoot aggregateRoot)
        {
            Task.Factory.StartNew(() => {
                using (var context = _dataContextFactory.Create()) {
                    context.SaveOrUpdate(aggregateRoot);
                    context.Commit();
                }
            }).Wait();

            var aggregateRootType = aggregateRoot.GetType();
            var eventPublisher = aggregateRoot as IEventPublisher;
            if (eventPublisher == null) {
                if (LogManager.Default.IsDebugEnabled)
                    LogManager.Default.DebugFormat("The aggregate root {0} of id {1} is saved.",
                        aggregateRootType.FullName, aggregateRoot.Id);

                return;
            }

            var events = eventPublisher.Events;
            if (events.IsEmpty())
                return;

            _messageBus.Publish(events);

            if (LogManager.Default.IsDebugEnabled)
                LogManager.Default.DebugFormat("The aggregate root {0} of id {1} is saved then publish all events [{2}].",
                    aggregateRootType.FullName, aggregateRoot.Id,
                    string.Join("|", events.Select(item => item.ToString())));
        }

        public override void Delete(IAggregateRoot aggregateRoot)
        {
            Task.Factory.StartNew(() => {
                using (var context = _dataContextFactory.Create()) {
                    context.Delete(aggregateRoot);
                    context.Commit();
                }
            }).Wait();

            if (LogManager.Default.IsDebugEnabled)
                LogManager.Default.DebugFormat("The aggregate root {0} of id {1} is deleted.",
                    aggregateRoot.GetType(), aggregateRoot.Id);
        }
    }
}
