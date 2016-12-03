using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ThinkLib;
using ThinkLib.Composition;
using ThinkLib.Scheduling;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Messaging.Handling;
using ThinkNet.Runtime;

namespace ThinkNet.Database.Storage
{
    /// <summary>
    /// 已发布事件的版本存储器
    /// </summary>
    public class EventPublishedVersionStore : EventPublishedVersionInMemory, IInitializer, IProcessor
    {
        private readonly IDataContextFactory _contextFactory;
        private readonly ConcurrentQueue<KeyValuePair<DataKey, int>> _queue;
        private readonly TimeScheduler _scheduler;
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public EventPublishedVersionStore(IDataContextFactory contextFactory)
        {
            this._contextFactory = contextFactory;
            this._queue = new ConcurrentQueue<KeyValuePair<DataKey, int>>();

            this._scheduler = TimeScheduler.Create("Publishing Version Scheduler", BatchSaving).SetInterval(2000);
        }

        private EventPublishedVersion Transform(KeyValuePair<DataKey, int> kvp)
        {
            var aggregateRootTypeName = kvp.Key.GetSourceTypeName();
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();
            return new EventPublishedVersion(aggregateRootTypeCode, kvp.Key.Id, kvp.Value, aggregateRootTypeName);
        }

        private void BatchSaving()
        {
            Dictionary<DataKey, int> dict = new Dictionary<DataKey, int>();
            KeyValuePair<DataKey, int> item;
            while(dict.Count < 20 && _queue.TryDequeue(out item)) {
                dict[item.Key] = item.Value;
            }

            if(dict.Count == 0)
                return;


            using(var context = _contextFactory.Create()) {
                dict.Select(Transform).ForEach(context.SaveOrUpdate);

                //var versionData = context.Find<EventPublishedVersion>(new object[] { aggregateRootTypeCode, sourceKey.Id });

                //if(versionData == null) {
                //    context.Save(new EventPublishedVersion(aggregateRootTypeCode, sourceKey.Id, version, sourceKey.GetSourceTypeFullName()));
                //}
                //else if(versionData.Version + 1 == version) {
                //    versionData.Version = version;
                //}
                //else {
                //    return;
                //}
                context.Commit();
            }
        }

        /// <summary>
        /// 添加或更新溯源聚合的版本号
        /// </summary>
        public override void AddOrUpdatePublishedVersion(DataKey sourceKey, int version)
        {
            _queue.Enqueue(new KeyValuePair<DataKey, int>(sourceKey, version));
        }

        /// <summary>
        /// 获取已发布的溯源聚合版本号
        /// </summary>
        public override int GetPublishedVersion(DataKey sourceKey)
        {
            var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            return Task.Factory.StartNew(() => {
                using(var context = _contextFactory.Create()) {
                    var data = context.Find<EventPublishedVersion>(new object[] { aggregateRootTypeCode, sourceKey.Id });
                    if(data != null) {
                        return data.Version;
                    }
                    return 0;
                }
            }).Result;
        }

        void IInitializer.Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            container.RegisterInstance<IProcessor>(this, "publishevent");
        }

        void IProcessor.Start()
        {
            _scheduler.Start();
        }

        void IProcessor.Stop()
        {
            _scheduler.Stop();
        }
    }
}
