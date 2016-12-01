﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinkLib;
using ThinkLib.Serialization;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Messaging;


namespace ThinkNet.Database.Storage
{
    /// <summary>
    /// <see cref="IEventStore"/>的实现类
    /// </summary>
    public sealed class EventStore : IEventStore
    {
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, int>> _versionCache;
        private readonly IDataContextFactory _dataContextFactory;
        private readonly ITextSerializer _serializer;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventStore(IDataContextFactory dataContextFactory, ITextSerializer serializer)
        {
            this._dataContextFactory = dataContextFactory;
            this._serializer = serializer;
            this._versionCache = new ConcurrentDictionary<int, ConcurrentDictionary<string, int>>();
        }

        //private bool EventPersisted(IDataContext context, int aggregateRootTypeCode, string aggregateRootId, int version, string correlationId)
        //{
        //    var query = context.CreateQuery<EventData>();
        //    if(!query.Any(p => p.CorrelationId == correlationId &&
        //            p.AggregateRootId == aggregateRootId &&
        //            p.AggregateRootTypeCode == aggregateRootTypeCode)) {
        //        return false;
        //    }

        //    return query.Any(p => p.AggregateRootId == aggregateRootId &&
        //            p.AggregateRootTypeCode == aggregateRootTypeCode &&
        //            p.Version == version);
        //}

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

        private bool Validate(int originalVersion, int sourceVersion, DataKey sourceId)
        {
            if(originalVersion + 1 < sourceVersion) {
                if(LogManager.Default.IsWarnEnabled)
                    LogManager.Default.WarnFormat("This eventstream was abandoned because the version '{0}' is less than the AggregateRoot version '{1}' on '{2}' of id '{3}'.",
                        sourceVersion, sourceVersion, sourceId.GetSourceTypeName(), sourceId.Id);
                return false;
            }

            if(originalVersion + 1 > sourceVersion) {
                if(LogManager.Default.IsWarnEnabled)
                    LogManager.Default.WarnFormat("This eventstream was abandoned because the version '{0}' is greater than the AggregateRoot version '{1}' on '{2}' of id '{3}'.",
                        sourceVersion, sourceVersion, sourceId.GetSourceTypeName(), sourceId.Id);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 保存事件流数据
        /// </summary>
        public void Save(EventStream @event)
        {
            var sourceTypeCode = @event.SourceId.GetSourceTypeName().GetHashCode();

            ConcurrentDictionary<string, int> aggregateRootVersion;
            int version;
            bool validated = false;
            if(@event.Version > 1 && _versionCache.TryGetValue(sourceTypeCode, out aggregateRootVersion) &&
                aggregateRootVersion.TryGetValue(@event.SourceId.Id, out version)) {
                validated = true;
                if(!Validate(version, @event.Version, @event.SourceId))
                    return;
            }


            Task.Factory.StartNew(delegate {
                using (var context = _dataContextFactory.Create()) {
                    if(@event.Version > 1 && !validated) {
                        version = context.CreateQuery<EventData>()
                            .Where(p => p.AggregateRootId == @event.SourceId.Id && p.AggregateRootTypeCode == sourceTypeCode)
                            .Max(p => p.Version);

                        if(!Validate(version, @event.Version, @event.SourceId))
                            return;
                    }
                    
                    var eventData = new EventData() {
                        AggregateRootId = @event.SourceId.Id,
                        AggregateRootTypeCode = @event.SourceId.GetSourceTypeName().GetHashCode(),
                        AggregateRootTypeName = @event.SourceId.GetSourceTypeFullName(),
                        CorrelationId = @event.CorrelationId,
                        Version = @event.Version,
                        Timestamp = DateTime.UtcNow,
                        Items = new List<EventDataItem>()
                    };
                    
                    //if(queryable.Any(p => p.CorrelationId == eventData.CorrelationId)) {
                    //    if(LogManager.Default.IsWarnEnabled)
                    //        LogManager.Default.WarnFormat("This eventstream was abandoned because the correlationId '{0}' is saved.",
                    //            eventData.CorrelationId);
                    //    return;
                    //}

                    @event.Events.Select(this.Transform).ForEach(eventData.AddItem);

                    context.Save(eventData);
                    context.Commit();
                }
            }).Wait();

            _versionCache.GetOrAdd(sourceTypeCode, () => new ConcurrentDictionary<string, int>())
                .AddOrUpdate(@event.SourceId.Id,
                    @event.Version,
                    (key, value) => @event.Version == value + 1 ? @event.Version : value);
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
                            p.AggregateRootId == sourceKey.Id &&
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
                        .Where(p => p.AggregateRootId == sourceKey.Id &&
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
                     .Where(p => p.AggregateRootId == sourceKey.Id &&
                         p.AggregateRootTypeCode == aggregateRootTypeCode)
                     .ToList()
                     .ForEach(context.Delete);
                    context.Commit();
                }
            }).Wait();
        }
    }
}