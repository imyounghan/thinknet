using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Database;
using ThinkNet.EventSourcing;
using ThinkNet.Messaging;


namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 事件存储器
    /// </summary>
    [Register(typeof(IEventStore))]
    public class EventStore : IEventStore
    {
        private readonly IDataContextFactory _dbContextFactory;
        private readonly ISerializer _serializer;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventStore(IDataContextFactory dbContextFactory, ISerializer serializer)
        {
            this._dbContextFactory = dbContextFactory;
            this._serializer = serializer;
        }

        private bool EventPersisted(IDataContext context, int aggregateRootTypeCode, string aggregateRootId, int version, string correlationId)
        {
            var query = context.CreateQuery<EventData>();
            if(!query.Any(p => p.CorrelationId == correlationId &&
                    p.AggregateRootId == aggregateRootId &&
                    p.AggregateRootTypeCode == aggregateRootTypeCode)) {
                return false;
            }

            return query.Any(p => p.AggregateRootId == aggregateRootId &&
                    p.AggregateRootTypeCode == aggregateRootTypeCode &&
                    p.Version == version);
        }

        private EventDataItem Transform(IEvent @event)
        {
            var type = @event.GetType();

            return new EventDataItem {
                AssemblyName = type.GetAssemblyName(),
                Namespace = type.Namespace,
                TypeName = type.Name,
                Payload = _serializer.SerializeToBinary(@event)
            };
        }

        private IEvent Transform(EventDataItem @event)
        {
            var typeName = string.Concat(@event.Namespace, ".", @event.TypeName, ", ", @event.AssemblyName);
            var type = Type.GetType(typeName);

            return (IEvent)_serializer.DeserializeFromBinary(@event.Payload, type);
        }

        private VersionedEvent Transform(EventData @event)
        {
            return new VersionedEvent(string.Empty, @event.Timestamp) {
                CommandId = @event.CorrelationId,
                SourceId = @event.AggregateRootId,
                SourceType = Type.GetType(@event.AggregateRootTypeName),
                Version = @event.Version,
                Events = @event.Items.OrderBy(p => p.Order).Select(this.Transform).ToArray()
            };
        }

        public void Save(VersionedEvent @event)
        {
            var aggregateRootTypeCode = @event.SourceType.FullName.GetHashCode();

            Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    int order = 0;
                    var eventItems = @event.Events.Select(this.Transform).ToList();
                    eventItems.ForEach(item => item.Order = ++order);

                    var data = new EventData() {
                        AggregateRootId = @event.SourceId,
                        AggregateRootTypeCode = aggregateRootTypeCode,
                        AggregateRootTypeName = @event.SourceType.GetFullName(),
                        CorrelationId = @event.CommandId,
                        Items = eventItems,
                        Version = @event.Version
                    };

                    context.Save(data);
                    context.Commit();
                }
            }).ContinueWith(task => {
                if(task.Status == TaskStatus.Faulted) {
                    if(LogManager.Default.IsErrorEnabled)
                        LogManager.Default.Error(task.Exception,
                            "events persistent failed. aggregateRootId:{0},aggregateRootType:{1},version:{2}.",
                            @event.SourceId, @event.SourceType.FullName, @event.Version);
                    throw task.Exception;
                }
                else {
                    if(LogManager.Default.IsDebugEnabled)
                        LogManager.Default.DebugFormat("events persistent completed. aggregateRootId:{0}, aggregateRootType:{1}, commandId:{2}.",
                            @event.SourceId, @event.SourceType.FullName, @event.CommandId);
                }
            });
        }

        public VersionedEvent Find(DataKey sourceKey, string correlationId)
        {
            correlationId.NotNullOrWhiteSpace("correlationId");

            var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            var @event = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    return context.CreateQuery<EventData>()
                        .Where(p => p.CorrelationId == correlationId &&
                            p.AggregateRootId == sourceKey.SourceId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode)
                        .FirstOrDefault();
                }
            }).Result;

            if(@event == null) {
                return null;
            }

            return new VersionedEvent(string.Empty, @event.Timestamp) {
                CommandId = correlationId,
                SourceId = @event.AggregateRootId,
                SourceType = Type.GetType(@event.AggregateRootTypeName),
                Version = @event.Version,
                Events = @event.Items.Select(this.Transform).ToArray()
            };
        }

        public IEnumerable<VersionedEvent> FindAll(DataKey sourceKey, int version)
        {
            var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            var events = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    return context.CreateQuery<EventData>()
                        .Where(p => p.AggregateRootId == sourceKey.SourceId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode &&
                            p.Version > version)
                        .OrderBy(p => p.Version)//.ThenBy(p => p.Order)
                        .ToList();
                }
            }).Result;

            return events.Select(this.Transform).ToArray();
        }

        public void RemoveAll(DataKey sourceKey)
        {
            var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    context.CreateQuery<EventData>()
                     .Where(p => p.AggregateRootId == sourceKey.SourceId &&
                         p.AggregateRootTypeCode == aggregateRootTypeCode)
                     .ToList()
                     .ForEach(context.Delete);
                    context.Commit();
                }
            }).Wait();
        }
    }
}