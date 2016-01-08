using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Infrastructure;


namespace ThinkNet.EventSourcing
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


        public void Save(SourceKey sourceKey, string correlationId, IDictionary<int, string> events)
        {
            var aggregateRootTypeCode = sourceKey.SourceTypeName.GetHashCode();
            var aggregateRootId = sourceKey.SourceId;

            var data = events.Select(item => new Event {
                AggregateRootId = aggregateRootId,
                AggregateRootTypeCode = aggregateRootTypeCode,
                Version = item.Key,
                CorrelationId = correlationId,
                Payload = item.Value,
                Timestamp = DateTime.UtcNow
            }).AsEnumerable();

            Task.Factory.StartNew(Save, data).Wait();
        }

        private void Save(object eventdata)
        {
            using (var context = _dbContextFactory.CreateDataContext()) {
                (eventdata as IEnumerable<Event>).ForEach(context.Save);
                context.Commit();
            }
        }

        public bool EventPersisted(SourceKey sourceKey, string correlationId)
        {
            Ensure.NotNullOrWhiteSpace(correlationId, "correlationId");

            using (var context = _dbContextFactory.CreateDataContext()) {
                return context.CreateQuery<Event>()
                    .Any(p => p.CorrelationId == correlationId &&
                        p.AggregateRootId == sourceKey.SourceId &&
                        p.AggregateRootTypeCode == sourceKey.SourceTypeName.GetHashCode());
            }
        }

        public IEnumerable<string> FindAll(SourceKey sourceKey, string correlationId)
        {
            Ensure.NotNullOrWhiteSpace(correlationId, "correlationId");

            using (var context = _dbContextFactory.CreateDataContext()) {
                return context.CreateQuery<Event>()
                    .Where(p => p.CorrelationId == correlationId &&
                        p.AggregateRootId == sourceKey.SourceId &&
                        p.AggregateRootTypeCode == sourceKey.SourceTypeName.GetHashCode())
                    .OrderBy(p => p.Version)
                    .ToList()
                    .Select(item => item.Payload);
            }
        }

        public IEnumerable<string> FindAll(SourceKey sourceKey, int version)
        {
            using (var context = _dbContextFactory.CreateDataContext()) {
                return context.CreateQuery<Event>()
                    .Where(p => p.AggregateRootId == sourceKey.SourceId &&
                        p.AggregateRootTypeCode == sourceKey.SourceTypeName.GetHashCode() &&
                        p.Version > version)
                    .ToList()
                    .Select(item => item.Payload);
            }
        }

        public void RemoveAll(SourceKey sourceKey)
        {
            using (var context = _dbContextFactory.CreateDataContext()) {
                context.CreateQuery<Event>()
                    .Where(p => p.AggregateRootId == sourceKey.SourceId &&
                        p.AggregateRootTypeCode == sourceKey.SourceTypeName.GetHashCode())
                    .ToList()
                    .ForEach(context.Delete);
                context.Commit();
            }
        }
    }
}
