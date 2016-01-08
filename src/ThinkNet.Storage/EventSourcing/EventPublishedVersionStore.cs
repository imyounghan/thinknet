using ThinkNet.Annotation;
using ThinkNet.Infrastructure;

namespace ThinkNet.EventSourcing
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
        public override void AddOrUpdatePublishedVersion(int aggregateRootTypeCode, string aggregateRootId, int startVersion, int endVersion)
        {
                using (var context = _contextFactory.CreateDataContext()) {
                    var versionData = context.Get<EventPublishedVersion>(new object[] { 
                        aggregateRootTypeCode, 
                        aggregateRootId
                    });

                    if (versionData == null) {
                        context.Save(new EventPublishedVersion {
                            AggregateRootId = aggregateRootId,
                            AggregateRootTypeCode = aggregateRootTypeCode,
                            Version = endVersion
                        });
                    }
                    else if (versionData.Version + 1 == startVersion) {
                        versionData.Version = endVersion;
                    }
                    context.Commit();
                }
        }

        /// <summary>
        /// 获取已发布的溯源聚合版本号
        /// </summary>
        public override int GetPublishedVersion(int aggregateRootTypeCode, string aggregateRootId)
        {
            int version = 0;
            using (var context = _contextFactory.CreateDataContext()) {
                var data = context.Get<EventPublishedVersion>(new object[] { 
                        aggregateRootTypeCode,
                        aggregateRootId
                    });
                if (data != null) {
                    version = data.Version;
                }
            }

            return version;
        }
    }
}
