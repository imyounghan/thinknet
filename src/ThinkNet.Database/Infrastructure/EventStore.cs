using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Database;


namespace ThinkNet.Infrastructure
{
    /// <summary>
    /// 事件存储器
    /// </summary>
    [Register(typeof(IEventStore))]
    public class EventStore : IEventStore
    {
        private readonly IDataContextFactory _dbContextFactory;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventStore(IDataContextFactory dbContextFactory)
        {
            this._dbContextFactory = dbContextFactory;
        }

        private bool EventPersisted(IDataContext context, int aggregateRootTypeCode, string aggregateRootId, string correlationId)
        {
            return context.CreateQuery<Event>()
                .Any(p => p.CorrelationId == correlationId &&
                    p.AggregateRootId == aggregateRootId &&
                    p.AggregateRootTypeCode == aggregateRootTypeCode);
        }


        public bool Save(DataKey sourceKey, string correlationId, IEnumerable<DataStream> events)
        {
            correlationId.NotNullOrWhiteSpace("correlationId");

            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            return Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    if (EventPersisted(context, aggregateRootTypeCode, sourceKey.SourceId, correlationId))
                        return false;

                    int order = 0;
                    foreach (var stream in events) {
                        var @event = new Event {
                            AggregateRootId = sourceKey.SourceId,
                            AggregateRootTypeCode = aggregateRootTypeCode,
                            AggregateRootTypeName = aggregateRootTypeName,
                            Version = stream.Version,
                            CorrelationId = correlationId,
                            Payload = stream.Payload,
                            AssemblyName = stream.Key.AssemblyName,
                            Namespace = stream.Key.Namespace,
                            TypeName = stream.Key.TypeName,
                            EventId = stream.Key.SourceId,
                            Order = ++order,
                            Timestamp = DateTime.UtcNow
                        };
                        context.Save(@event);
                    }
                    context.Commit();

                    return true;
                }
            }).Result;
        }

        public IEnumerable<DataStream> FindAll(DataKey sourceKey, string correlationId)
        {
            correlationId.NotNullOrWhiteSpace("correlationId");

            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var events = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    return context.CreateQuery<Event>()
                        .Where(p => p.CorrelationId == correlationId &&
                            p.AggregateRootId == sourceKey.SourceId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode)
                        .ToList();
                }
            }).Result;

            return events.Select(item => new DataStream {
                Key = new DataKey(item.EventId, item.Namespace, item.TypeName, item.AssemblyName),
                Version = item.Version,
                Payload = item.Payload
            }).AsEnumerable();
        }

        public IEnumerable<DataStream> FindAll(DataKey sourceKey, int version)
        {
            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var events = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    return context.CreateQuery<Event>()
                        .Where(p => p.AggregateRootId == sourceKey.SourceId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode &&
                            p.Version > version)
                        .OrderBy(p => p.Version).ThenBy(p => p.Order)
                        .ToList();
                }
            }).Result;

            return events.Select(item => new DataStream {
                Key = new DataKey(item.EventId, item.Namespace, item.TypeName, item.AssemblyName),
                Version = item.Version,
                Payload = item.Payload
            }).AsEnumerable();
        }

        public void RemoveAll(DataKey sourceKey)
        {
            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    context.CreateQuery<Event>()
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