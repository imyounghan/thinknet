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
    /// <see cref="IEventStore"/>的实现类
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

        private EventDataItem Transform(Event @event)
        {
            var eventDataItem = new EventDataItem(@event.GetType());
            eventDataItem.Payload = _serializer.SerializeToBinary(@event);
            return eventDataItem;
        }

        private Event Transform(EventDataItem @event)
        {
            var typeName = string.Concat(@event.Namespace, ".", @event.TypeName, ", ", @event.AssemblyName);
            var type = Type.GetType(typeName);

            return (Event)_serializer.DeserializeFromBinary(@event.Payload, type);
        }

        private EventStream Transform(EventData @event)
        {
            return new EventStream() {
                CorrelationId = @event.CorrelationId,
                SourceId = new DataKey(@event.AggregateRootId, @event.AggregateRootTypeName),
                Version = @event.Version,
                Events = @event.Items.OrderBy(p => p.Order).Select(this.Transform).ToArray()
            };
        }

        /// <summary>
        /// 保存事件流数据
        /// </summary>
        public void Save(EventStream @event)
        {
            Task.Factory.StartNew(delegate {
                using (var context = _dataContextFactory.Create()) {
                    var eventData = new EventData(@event.SourceId) {
                        CorrelationId = @event.CorrelationId,
                        Version = @event.Version
                    };

                    var queryable = context.CreateQuery<EventData>();

                    queryable = queryable.Where(p => p.AggregateRootId == eventData.AggregateRootId &&
                        p.AggregateRootTypeCode == eventData.AggregateRootTypeCode);
                    int version = !queryable.Any() ? 0 : queryable.Max(p => p.Version);
                    if(version + 1 < eventData.Version) {
                        if(LogManager.Default.IsWarnEnabled)
                            LogManager.Default.WarnFormat("This eventstream was abandoned because the version '{0}' is less than the AggregateRoot version '{1}' on '{2}' of id '{3}'.",
                                eventData.Version, version, @event.SourceId.GetSourceTypeName(), @event.SourceId.UniqueId);
                        return;
                    }
                    else if(version + 1 > eventData.Version) {
                        if(LogManager.Default.IsWarnEnabled)
                            LogManager.Default.WarnFormat("This eventstream was abandoned because the version '{0}' is greater than the AggregateRoot version '{1}' on '{2}' of id '{3}'.",
                                eventData.Version, version, @event.SourceId.GetSourceTypeName(), @event.SourceId.UniqueId);
                        throw new ThinkNetException("");
                    }

                    if(queryable.Any(p => p.CorrelationId == eventData.CorrelationId)) {
                        if(LogManager.Default.IsWarnEnabled)
                            LogManager.Default.WarnFormat("This eventstream was abandoned because the correlationId '{0}' is saved.",
                                eventData.CorrelationId);
                        return;
                    }

                    @event.Events.Select(this.Transform).ForEach(eventData.AddItem);

                    context.Save(eventData);
                    context.Commit();
                }
            }).Wait();
        }

        /// <summary>
        /// 查找与该命令相关的事件流数据
        /// </summary>
        public EventStream Find(DataKey sourceKey, string correlationId)
        {
            correlationId.NotNullOrWhiteSpace("correlationId");

            var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            var @event = Task.Factory.StartNew(delegate {
                using (var context = _dataContextFactory.Create()) {
                    return context.CreateQuery<EventData>()
                        .Where(p => p.CorrelationId == correlationId &&
                            p.AggregateRootId == sourceKey.UniqueId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode)
                        .FirstOrDefault();
                }
            }).Result;

            if(@event == null) {
                return null;
            }

            return new EventStream() {
                CorrelationId = correlationId,
                SourceId = new DataKey(@event.AggregateRootId, @event.AggregateRootTypeName),
                Version = @event.Version,
                Events = @event.Items.Select(this.Transform).ToArray()
            };
        }

        /// <summary>
        /// 查找大于该版本号的所有事件流数据
        /// </summary>
        public IEnumerable<EventStream> FindAll(DataKey sourceKey, int version)
        {
            var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            var events = Task.Factory.StartNew(delegate {
                using (var context = _dataContextFactory.Create()) {
                    return context.CreateQuery<EventData>()
                        .Where(p => p.AggregateRootId == sourceKey.UniqueId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode &&
                            p.Version > version)
                        .OrderBy(p => p.Version)//.ThenBy(p => p.Order)
                        .ToList();
                }
            }).Result;

            return events.Select(this.Transform).ToArray();
        }

        /// <summary>
        /// 删除相关的事件流数据
        /// </summary>
        public void RemoveAll(DataKey sourceKey)
        {
            var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            Task.Factory.StartNew(delegate {
                using (var context = _dataContextFactory.Create()) {
                    context.CreateQuery<EventData>()
                     .Where(p => p.AggregateRootId == sourceKey.UniqueId &&
                         p.AggregateRootTypeCode == aggregateRootTypeCode)
                     .ToList()
                     .ForEach(context.Delete);
                    context.Commit();
                }
            }).Wait();
        }
    }
}