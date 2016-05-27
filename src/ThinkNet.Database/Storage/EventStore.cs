using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Configurations;
using ThinkNet.EventSourcing;


namespace ThinkNet.Database.Storage
{
    /// <summary>
    /// 事件存储器
    /// </summary>
    [RegisterComponent(typeof(IEventStore))]
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

        private void EventSaved(IDataContext context, 
            string aggregateRootTypeName, 
            int aggregateRootTypeCode, 
            string aggregateRootId, 
            string correlationId, 
            IEnumerable<Stream> events)
        {
            events.Select(item => new Event {
                AggregateRootId = aggregateRootId,
                AggregateRootTypeCode = aggregateRootTypeCode,
                AggregateRootTypeName = aggregateRootTypeName,
                Version = item.Version,
                CorrelationId = correlationId,
                Payload = item.Payload,
                AssemblyName = item.Key.AssemblyName,
                Namespace = item.Key.Namespace,
                TypeName = item.Key.TypeName,
                EventId = item.Key.SourceId,
                Timestamp = DateTime.UtcNow
            }).ForEach(context.Save);
            context.Commit();
        }


        public bool Save(SourceKey sourceKey, string correlationId, Func<IEnumerable<Stream>> eventsFactory)
        {
            correlationId.NotNullOrWhiteSpace("correlationId");

            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            return Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    if (EventPersisted(context, aggregateRootTypeCode, sourceKey.SourceId, correlationId))
                        return false;

                    EventSaved(context, aggregateRootTypeName, aggregateRootTypeCode, sourceKey.SourceId, correlationId, eventsFactory());

                    return true;
                }
            }).Result;
        }

        public IEnumerable<Stream> FindAll(SourceKey sourceKey, string correlationId)
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
                        .OrderBy(p => p.Version)
                        .ToList();

                    
                }
            }).Result;

            return events.Select(item => new Stream {
                Key = new SourceKey(item.EventId, item.Namespace, item.TypeName, item.AssemblyName),
                Version = item.Version,
                Payload = item.Payload
            }).AsEnumerable();
        }

        public IEnumerable<Stream> FindAll(SourceKey sourceKey, int version)
        {
            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var events = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    return context.CreateQuery<Event>()
                        .Where(p => p.AggregateRootId == sourceKey.SourceId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode &&
                            p.Version > version)
                        .OrderBy(p => p.Version)
                        .ToList();
                }
            }).Result;

            return events.Select(item => new Stream {
                Key = new SourceKey(item.EventId, item.Namespace, item.TypeName, item.AssemblyName),
                Version = item.Version,
                Payload = item.Payload
            }).AsEnumerable();
        }

        public void RemoveAll(SourceKey sourceKey)
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