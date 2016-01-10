using System.Threading.Tasks;
using ThinkNet.Common;
using ThinkNet.EventSourcing;

namespace ThinkNet.Database.Storage
{
    /// <summary>
    /// 存储聚合事件的发布版本号到数据库中
    /// </summary>
    [RegisterComponent(typeof(IEventPublishedVersionStore))]
    public class EventPublishedVersionStore : EventPublishedVersionInMemory
    {
        private readonly IDataContextFactory _contextFactory;
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public EventPublishedVersionStore(IDataContextFactory contextFactory)
        {
            this._contextFactory = contextFactory;
        }

        /// <summary>
        /// 添加或更新溯源聚合的版本号
        /// </summary>
        public override void AddOrUpdatePublishedVersion(SourceKey sourceKey, int startVersion, int endVersion)
        {
            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            Task.Factory.StartNew(() => {
                using (var context = _contextFactory.CreateDataContext()) {
                    var versionData = context.Find<EventPublishedVersion>(aggregateRootTypeCode, sourceKey.SourceId);

                    if (versionData == null) {
                        context.Save(new EventPublishedVersion(aggregateRootTypeCode, sourceKey.SourceId, endVersion));
                    }
                    else if (versionData.Version + 1 == startVersion) {
                        versionData.Version = endVersion;
                    }
                    context.Commit();
                }
            }).Wait();
        }

        /// <summary>
        /// 获取已发布的溯源聚合版本号
        /// </summary>
        public override int GetPublishedVersion(SourceKey sourceKey)
        {
            var aggregateRootTypeName = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName);
            var aggregateRootTypeCode = aggregateRootTypeName.GetHashCode();

            var task = Task.Factory.StartNew(() => {
                using (var context = _contextFactory.CreateDataContext()) {
                    var data = context.Find<EventPublishedVersion>(aggregateRootTypeCode, sourceKey.SourceId);
                    if (data != null) {
                        return data.Version;
                    }
                    return 0;
                }
            });
            task.Wait();

            return task.Result;
        }        
    }
}
