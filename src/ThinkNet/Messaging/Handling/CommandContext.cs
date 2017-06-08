

namespace ThinkNet.Messaging.Handling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ThinkNet.Infrastructure;
    using ThinkNet.Seeds;

    public class CommandContext : ICommandContext
    {
        private readonly Dictionary<string, IAggregateRoot> trackedAggregateRoots;

        private readonly IEventStore eventStore;
        private readonly ICache cache;
        private readonly ISnapshotStore snapshotStore;
        private readonly IEventBus eventBus;
        private readonly ILogger logger;

        private readonly IRepository repository;

        public CommandContext(IEventBus eventBus,
            IEventStore eventStore,
            ISnapshotStore snapshotStore,
            IRepository repository,
            ICache cache,
            ILogger logger)
        {
            this.eventBus = eventBus;
            this.eventStore = eventStore;
            this.snapshotStore = snapshotStore;
            this.repository = repository;
            this.cache = cache;
            this.logger = logger;

            this.trackedAggregateRoots = new Dictionary<string, IAggregateRoot>();
        }

        public string CommandId { get; set; }

        public ICommand Command { get; set; }

        public TraceInfo TraceInfo { get; set; }

        public void Add(IAggregateRoot aggregateRoot)
        {
            aggregateRoot.NotNull("aggregateRoot");

            string key = string.Concat(aggregateRoot.GetType().FullName, "@", aggregateRoot.Id);
            trackedAggregateRoots.TryAdd(key, aggregateRoot);
        }

        public TEventSourced Get<TEventSourced, TIdentify>(TIdentify id) where TEventSourced : class, IEventSourced
        {
            var aggregateRoot = this.Find<TEventSourced, TIdentify>(id);
            if(aggregateRoot == null)
                throw new EntityNotFoundException(id, typeof(TEventSourced));

            return aggregateRoot;
        }

        public TAggregateRoot Find<TAggregateRoot, TIdentify>(TIdentify id) where TAggregateRoot : class, IAggregateRoot
        {
            var type = typeof(TAggregateRoot);
            if(!type.IsClass || type.IsAbstract) {
                string errorMessage = string.Format("The type of '{0}' must be a non abstract class.", type.FullName);
                throw new ApplicationException(errorMessage);
            }

            string key = string.Concat(type.FullName, "@", id);

            IAggregateRoot aggregateRoot;
            if(!trackedAggregateRoots.TryGetValue(key, out aggregateRoot)) {
                if (type.IsAssignableFrom(typeof(IEventSourced)))
                {
                    aggregateRoot = this.Restore(type, id);
                }
                else
                {
                    aggregateRoot = this.repository.Find(type, id);
                }

                if(aggregateRoot != null) {
                    trackedAggregateRoots.Add(key, aggregateRoot);
                }
            }

            return aggregateRoot as TAggregateRoot;
        }

        private IEventSourced Create(Type sourceType, object sourceId)
        {
            var idType = sourceId.GetType();
            var constructor = sourceType.GetConstructor(new[] { idType });

            if(constructor == null) {
                string errorMessage = string.Format("Type '{0}' must have a constructor with the following signature: .ctor({1} id)", sourceType.FullName, idType.FullName);
                throw new ApplicationException(errorMessage);
                //return FormatterServices.GetUninitializedObject(aggregateRootType) as IEventSourced;
            }

            return constructor.Invoke(new[] { sourceId }) as IEventSourced;
        }

        /// <summary>
        /// 根据主键获取聚合根实例。
        /// </summary>
        private IEventSourced Restore(Type sourceType, object sourceId)
        {
            sourceId.NotNull("sourceId");

            IEventSourced aggregateRoot = null;
            if(this.cache.TryGet(sourceType, sourceId, out aggregateRoot)) {
                if(this.logger.IsDebugEnabled)
                    this.logger.DebugFormat("Find the aggregate root {0} of id {1} from cache.",
                        sourceType.FullName, sourceId);

                return aggregateRoot;
            }

            try {
                aggregateRoot = this.snapshotStore.GetLastest(sourceType, sourceId) as IEventSourced;
                if(aggregateRoot != null) {
                    if(this.logger.IsDebugEnabled)
                        this.logger.DebugFormat("Find the aggregate root '{0}' of id '{1}' from snapshot. current version:{2}.",
                            sourceType.FullName, sourceId, aggregateRoot.Version);
                }
            }
            catch(Exception ex) {
                if(this.logger.IsWarnEnabled)
                    this.logger.Warn(ex,
                        "Get the latest snapshot failed. aggregateRootId:{0},aggregateRootType:{1}.",
                        sourceId, sourceType.FullName);
            }


            var events = this.eventStore.FindAll(new SourceInfo(sourceId, sourceType), aggregateRoot.Version);
            if(!events.IsEmpty()) {
                if(aggregateRoot == null) {
                    aggregateRoot = this.Create(sourceType, sourceId);
                }

                foreach(var @event in events) {
                    aggregateRoot.LoadFrom(@event);
                    aggregateRoot.AcceptChanges(@event.Version);
                }

                if(this.logger.IsDebugEnabled)
                    this.logger.DebugFormat("Restore the aggregate root '{0}' of id '{1}' from event stream. version:{2} ~ {3}",
                        sourceType.FullName,
                        sourceId,
                        events.Min(p => p.Version),
                        events.Max(p => p.Version));
            }

            if (aggregateRoot != null)
            {
                this.cache.Set(aggregateRoot, sourceId);
            }

            return aggregateRoot;
        }

        public void Commit()
        {
            var dirtyAggregateRootCount = 0;
            var dirtyAggregateRoot = default(IEventSourced);
            var changedEvents = Enumerable.Empty<IEvent>();
            foreach (var aggregateRoot in trackedAggregateRoots.Values.OfType<IEventSourced>())
            {
                changedEvents = aggregateRoot.GetEvents();
                if (!changedEvents.IsEmpty())
                {
                    dirtyAggregateRootCount++;
                    if (dirtyAggregateRootCount > 1)
                    {
                        var errorMessage =
                            string.Format(
                                "Detected more than one aggregate created or modified by command. commandType:{0}, commandId:{1}",
                                this.Command.GetType().FullName,
                                this.CommandId);
                        throw new ApplicationException(errorMessage);
                    }
                    dirtyAggregateRoot = aggregateRoot;
                }
            }

            if(dirtyAggregateRootCount == 0 || changedEvents.IsEmpty()) {
                var errorMessage = string.Format("Not found aggregate to be created or modified by command. commandType:{0}, commandId:{1}",
                    this.Command.GetType().FullName,
                    this.CommandId);
                throw new ApplicationException(errorMessage);
            }

            var aggregateRootType = dirtyAggregateRoot.GetType();

            var sourceInfo = new SourceInfo(dirtyAggregateRoot.Id, aggregateRootType);
            var aggregateRootVersion = dirtyAggregateRoot.Version + 1;

            var envelopedCommand = new Envelope<ICommand>(this.Command)
                                       {
                                           MessageId = this.CommandId,
                                           CorrelationId = dirtyAggregateRoot.Id
                                       };
            envelopedCommand.Items["TraceInfo"] = this.TraceInfo;
            //envelopedCommand.Items["SourceKey"] = sourceInfo;

            var eventCollection = new EventCollection(aggregateRootVersion, this.CommandId, changedEvents);
            try
            {
                if(this.eventStore.Save(sourceInfo, eventCollection)) {
                    dirtyAggregateRoot.AcceptChanges(aggregateRootVersion);

                    if(this.logger.IsDebugEnabled)
                        this.logger.DebugFormat("Persistent domain events success. aggregateRootType:{0},aggregateRootId:{1},version:{2}.",
                            dirtyAggregateRoot.Id, aggregateRootType.FullName, dirtyAggregateRoot.Version);
                }
                else
                {
                    eventCollection = this.eventStore.Find(sourceInfo, this.CommandId);

                    this.eventBus.Publish(sourceInfo, eventCollection, envelopedCommand);
                    return;
                }
            }
            catch(Exception ex) {
                if(this.logger.IsErrorEnabled)
                    this.logger.Error(ex,
                        "Persistent domain events failed. aggregateRootType:{0},aggregateRootId:{1},version:{2}.",
                        dirtyAggregateRoot.Id, aggregateRootType.FullName, dirtyAggregateRoot.Version);
                throw ex;
            }


            try {
                this.cache.Set(dirtyAggregateRoot, dirtyAggregateRoot.Id);
            }
            catch(Exception ex) {
                if(this.logger.IsErrorEnabled)
                    this.logger.Error(ex,
                        "Refresh aggregate memory cache failed. aggregateRootType:{0},aggregateRootId:{1},commandId:{2}.",
                        dirtyAggregateRoot.Id, aggregateRootType.FullName, this.CommandId);
            }


            this.eventBus.Publish(sourceInfo, eventCollection, envelopedCommand);
        }
    }
}
