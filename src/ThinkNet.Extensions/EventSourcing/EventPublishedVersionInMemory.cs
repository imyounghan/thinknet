using System.Collections.Concurrent;

namespace ThinkNet.EventSourcing
{
    /// <summary>
    /// 存储聚合事件的发布版本号到内存中
    /// </summary>
    public class EventPublishedVersionInMemory : IEventPublishedVersionStore
    {
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, int>> _versionCache;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public EventPublishedVersionInMemory()
        {
            this._versionCache = new ConcurrentDictionary<int, ConcurrentDictionary<string, int>>();
        }

        /// <summary>
        /// 添加或更新溯源聚合的版本号
        /// </summary>
        public virtual void AddOrUpdatePublishedVersion(int aggregateRootTypeCode, string aggregateRootId, int startVersion, int endVersion)
        { }

        private void AddOrUpdatePublishedVersionToMemory(int aggregateRootTypeCode, string aggregateRootId, int startVersion, int endVersion)
        {
            _versionCache.GetOrAdd(aggregateRootTypeCode, typeCode => new ConcurrentDictionary<string, int>())
                .AddOrUpdate(aggregateRootId,
                    key => endVersion,
                    (key, version) => {
                        if (version + 1 == startVersion)
                            return version;

                        return endVersion;
                    });
        }

        /// <summary>
        /// 获取已发布的溯源聚合版本号
        /// </summary>
        public virtual int GetPublishedVersion(int aggregateRootTypeCode, string aggregateRootId)
        {
            return 0;
        }

        private int GetPublishedVersionFromMemory(int aggregateRootTypeCode, string aggregateRootId)
        {
            ConcurrentDictionary<string, int> dict;
            if (_versionCache.TryGetValue(aggregateRootTypeCode, out dict)) {
                int version;
                if (dict.TryGetValue(aggregateRootId, out version)) {
                    return version;
                }
            }            

            return -1;
        }

        #region IEventPublishedVersionStore 成员

        void IEventPublishedVersionStore.AddOrUpdatePublishedVersion(string aggregateRootType, string aggregateRootId, int startVersion, int endVersion)
        {
            this.AddOrUpdatePublishedVersion(aggregateRootType.GetHashCode(), aggregateRootId, startVersion, endVersion);
            this.AddOrUpdatePublishedVersionToMemory(aggregateRootType.GetHashCode(), aggregateRootId, startVersion, endVersion);
        }

        int IEventPublishedVersionStore.GetPublishedVersion(string aggregateRootType, string aggregateRootId)
        {
            var version = this.GetPublishedVersionFromMemory(aggregateRootType.GetHashCode(), aggregateRootId);

            if (version < 0) {
                version = this.GetPublishedVersion(aggregateRootType.GetHashCode(), aggregateRootId);
                this.AddOrUpdatePublishedVersionToMemory(aggregateRootType.GetHashCode(), aggregateRootId, 0, version);
            }

            return version;
        }

        #endregion
    }
}
