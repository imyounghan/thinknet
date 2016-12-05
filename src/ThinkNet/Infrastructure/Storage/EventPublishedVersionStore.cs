using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ThinkNet.Database;
using ThinkNet.Messaging.Handling;
using ThinkNet.Runtime;

namespace ThinkNet.Infrastructure.Storage
{
    /// <summary>
    /// 已发布事件的版本存储器
    /// </summary>
    public class EventPublishedVersionStore : EventPublishedVersionInMemory, IInitializer, IProcessor
    {
        private readonly IDataContextFactory _contextFactory;
        private readonly ConcurrentQueue<KeyValuePair<SourceKey, int>> _queue;
        //private readonly TimeScheduler _scheduler;
        private Timer _scheduler;
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public EventPublishedVersionStore(IDataContextFactory contextFactory)
        {
            this._contextFactory = contextFactory;
            this._queue = new ConcurrentQueue<KeyValuePair<SourceKey, int>>();

            //this._scheduler = TimeScheduler.Create("Publishing Version Scheduler", BatchSaving).SetInterval(2000);
        }

        private EventPublishedVersion Transform(KeyValuePair<SourceKey, int> kvp)
        {
            var aggregateRootTypeName = kvp.Key.GetSourceTypeName();
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();
            return new EventPublishedVersion(aggregateRootTypeCode, kvp.Key.Id, kvp.Value, aggregateRootTypeName);
        }

        private void BatchSaving(object state)
        {
            Dictionary<SourceKey, int> dict = new Dictionary<SourceKey, int>();
            KeyValuePair<SourceKey, int> item;
            while(dict.Count < 20 && _queue.TryDequeue(out item)) {
                dict[item.Key] = item.Value;
            }

            if(dict.Count == 0)
                return;


            try {
                using(var context = _contextFactory.Create()) {
                    dict.Select(Transform).ForEach(context.SaveOrUpdate);
                    context.Commit();
                }
            }
            catch(Exception ex) {
                if(LogManager.Default.IsErrorEnabled) {
                    LogManager.Default.Error("", ex);
                }
            }
            
        }

        /// <summary>
        /// 添加或更新溯源聚合的版本号
        /// </summary>
        public override void AddOrUpdatePublishedVersion(SourceKey sourceKey, int version)
        {
            _queue.Enqueue(new KeyValuePair<SourceKey, int>(sourceKey, version));
        }

        /// <summary>
        /// 获取已发布的溯源聚合版本号
        /// </summary>
        public override int GetPublishedVersion(SourceKey sourceKey)
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
            if(_scheduler == null) {
                _scheduler = new Timer(BatchSaving, null, 5000, 2000);
            }
            //_scheduler.Start();
        }

        void IProcessor.Stop()
        {
            if(_scheduler != null) {
                _scheduler.Dispose();
                _scheduler = null;
            }
            //_scheduler.Stop();
        }
    }
}
