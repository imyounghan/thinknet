using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Common;
using ThinkNet.Common.Serialization;
using ThinkNet.Database;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Messaging;


namespace ThinkNet.Runtime.Writing
{
    /// <summary>
    /// 事件存储器
    /// </summary>
    public sealed class EventStore : IEventStore
    {
        private readonly IDataContextFactory _dataContextFactory;
        private readonly ITextSerializer _serializer;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventStore(IDataContextFactory dataContextFactory, ITextSerializer serializer)
        {
            this._dataContextFactory = dataContextFactory;
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
            var eventDataItem = new EventDataItem(@event.GetType());
            eventDataItem.Payload = _serializer.SerializeToBinary(@event);
            return eventDataItem;
        }

        private IEvent Transform(EventDataItem @event)
        {
            var typeName = string.Concat(@event.Namespace, ".", @event.TypeName, ", ", @event.AssemblyName);
            var type = Type.GetType(typeName);

            return (IEvent)_serializer.DeserializeFromBinary(@event.Payload, type);
        }

        private EventStream Transform(EventData @event)
        {
            return new EventStream() {
                CorrelationId = @event.CorrelationId,
                SourceId = @event.AggregateRootId,
                SourceType = Type.GetType(@event.AggregateRootTypeName),
                Version = @event.Version,
                Events = @event.Items.OrderBy(p => p.Order).Select(this.Transform).ToArray()
            };
        }

        public void Save(EventStream @event)
        {
            Task.Factory.StartNew(delegate {
                using (var context = _dataContextFactory.Create()) {
                    var eventData = new EventData(@event.SourceType, @event.SourceId) {
                        CorrelationId = @event.CorrelationId,
                        Version = @event.Version
                    };

                    var queryable = context.CreateQuery<EventData>();
                    if (queryable.Where(p => p.AggregateRootId == eventData.AggregateRootId &&
                        p.AggregateRootTypeCode == eventData.AggregateRootTypeCode).Max(p => p.Version) != eventData.Version) {
                        //TODO..表示修改聚合时产生了并发
                        throw new ThinkNetException("");
                    }

                    if (queryable.Any(p => p.CorrelationId == eventData.CorrelationId &&
                        p.AggregateRootId == eventData.AggregateRootId &&
                        p.AggregateRootTypeCode == eventData.AggregateRootTypeCode)) {
                        //TODO..表示该命令产生的相关领域事件已保存，写相关警告日志
                        return;
                    }

                    @event.Events.Select(this.Transform).ForEach(eventData.AddItem);

                    context.Save(eventData);
                    context.Commit();
                }
            }).Wait();
        }

        public EventStream Find(DataKey sourceKey, string correlationId)
        {
            correlationId.NotNullOrWhiteSpace("correlationId");

            var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            var @event = Task.Factory.StartNew(delegate {
                using (var context = _dataContextFactory.Create()) {
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

            return new EventStream() {
                CorrelationId = correlationId,
                SourceId = @event.AggregateRootId,
                SourceType = Type.GetType(@event.AggregateRootTypeName),
                Version = @event.Version,
                Events = @event.Items.Select(this.Transform).ToArray()
            };
        }

        public IEnumerable<EventStream> FindAll(DataKey sourceKey, int version)
        {
            var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            var events = Task.Factory.StartNew(delegate {
                using (var context = _dataContextFactory.Create()) {
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

            Task.Factory.StartNew(delegate {
                using (var context = _dataContextFactory.Create()) {
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