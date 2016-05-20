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
        public virtual void AddOrUpdatePublishedVersion(SourceKey sourceKey, int startVersion, int endVersion)
        { }

        private void AddOrUpdatePublishedVersionToMemory(SourceKey sourceKey, int startVersion, int endVersion)
        {
            var sourceTypeCode = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName).GetHashCode();

            _versionCache.GetOrAdd(sourceTypeCode, typeCode => new ConcurrentDictionary<string, int>())
                .AddOrUpdate(sourceKey.SourceId,
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
        public virtual int GetPublishedVersion(SourceKey sourceKey)
        {
            return 0;
        }

        private int GetPublishedVersionFromMemory(SourceKey sourceKey)
        {
            ConcurrentDictionary<string, int> dict;
            var sourceTypeCode = string.Concat(sourceKey.Namespace, ".", sourceKey.TypeName).GetHashCode();
            if (_versionCache.TryGetValue(sourceTypeCode, out dict)) {
                int version;
                if (dict.TryGetValue(sourceKey.SourceId, out version)) {
                    return version;
                }
            }     

            return -1;
        }

        #region IEventPublishedVersionStore 成员

        void IEventPublishedVersionStore.AddOrUpdatePublishedVersion(SourceKey sourceKey, int startVersion, int endVersion)
        {
            this.AddOrUpdatePublishedVersionToMemory(sourceKey, startVersion, endVersion);
            this.AddOrUpdatePublishedVersion(sourceKey, startVersion, endVersion);            
        }

        int IEventPublishedVersionStore.GetPublishedVersion(SourceKey sourceKey)
        {
            var version = this.GetPublishedVersionFromMemory(sourceKey);

            if (version < 0) {
                version = this.GetPublishedVersion(sourceKey);
                this.AddOrUpdatePublishedVersionToMemory(sourceKey, 0, version);
            }

            return version;
        }

        #endregion
    }
}
