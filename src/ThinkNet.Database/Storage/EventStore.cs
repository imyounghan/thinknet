using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.EventSourcing;
using ThinkLib.Common;


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


        public void Save(SourceKey sourceKey, string correlationId, IEnumerable<Stream> events)
        {
            correlationId.NotNullOrWhiteSpace("correlationId");

            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var datas = events.Select(item => new Event {
                AggregateRootId = sourceKey.SourceId,
                AggregateRootTypeCode = aggregateRootTypeCode,
                AggregateRootTypeName = aggregateRootTypeName,//string.Concat(aggregateRootTypeName, ", ", sourceKey.AssemblyName),
                Version = item.Version,
                CorrelationId = correlationId,
                Payload = item.Payload,
                AssemblyName = item.Key.AssemblyName,
                Namespace = item.Key.Namespace,
                TypeName = item.Key.TypeName,
                EventId = item.Key.SourceId,
                Timestamp = DateTime.UtcNow
            }).AsEnumerable();

            Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    datas.ForEach(context.Save);
                    context.Commit();
                }
            }).Wait();
        }

        IEnumerable<Stream> IEventStore.FindAll(SourceKey sourceKey, string correlationId)
        {
            correlationId.NotNullOrWhiteSpace("correlationId");

            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var task = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    var events = context.CreateQuery<Event>()
                        .Where(p => p.CorrelationId == correlationId &&
                            p.AggregateRootId == sourceKey.SourceId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode)
                        .OrderBy(p => p.Version)
                        .ToList();

                    return events.Select(item => new Stream {
                        Key = new SourceKey(item.EventId, item.Namespace, item.TypeName, item.AssemblyName),
                        Version = item.Version,
                        Payload = item.Payload
                    }).AsEnumerable();
                }
            });
            task.Wait();

            return task.Result;
        }

        IEnumerable<Stream> IEventStore.FindAll(SourceKey sourceKey, int version)
        {
            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var task = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    var events = context.CreateQuery<Event>()
                        .Where(p => p.AggregateRootId == sourceKey.SourceId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode &&
                            p.Version > version)
                        .OrderBy(p => p.Version)
                        .ToList();

                    return events.Select(item => new Stream {
                        Key = new SourceKey(item.EventId, item.Namespace, item.TypeName, item.AssemblyName),
                        Version = item.Version,
                        Payload = item.Payload
                    }).AsEnumerable();
                }
            });
            task.Wait();

            return task.Result;
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


        public bool EventPersisted(SourceKey sourceKey, string correlationId)
        {
            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var task = Task.Factory.StartNew(() => {
                using (var context = _dbContextFactory.CreateDataContext()) {
                    return context.CreateQuery<Event>()
                        .Any(p => p.CorrelationId == correlationId &&
                            p.AggregateRootId == sourceKey.SourceId &&
                            p.AggregateRootTypeCode == aggregateRootTypeCode);
                }
            });
            task.Wait();

            return task.Result;
        }
    }
}