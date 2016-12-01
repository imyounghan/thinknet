using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ThinkNet.Domain.EventSourcing;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Database.Storage
{
    public class EventPublishedVersionStore : EventPublishedVersionInMemory
    {
        private readonly IDataContextFactory _contextFactory;
        private readonly HashSet<EventPublishedVersion> _queue;
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public EventPublishedVersionStore(IDataContextFactory contextFactory)
        {
            this._contextFactory = contextFactory;
            this._queue = new HashSet<EventPublishedVersion>();
        }

        /// <summary>
        /// 添加或更新溯源聚合的版本号
        /// </summary>
        public override void AddOrUpdatePublishedVersion(DataKey sourceKey, int version)
        {
            //_queue.Enqueue()
            //var aggregateRootTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            //Task.Factory.StartNew(() => {
            //    using(var context = _contextFactory.Create()) {
            //        var versionData = context.Find<EventPublishedVersion>(new object[] { aggregateRootTypeCode, sourceKey.Id });

            //        if(versionData == null) {
            //            context.Save(new EventPublishedVersion(aggregateRootTypeCode, sourceKey.Id, version, sourceKey.GetSourceTypeFullName()));
            //        }
            //        else if(versionData.Version + 1 == version) {
            //            versionData.Version = version;
            //        }
            //        else {
            //            return;
            //        }
            //        context.Commit();
            //    }
            //});
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
    }
}
