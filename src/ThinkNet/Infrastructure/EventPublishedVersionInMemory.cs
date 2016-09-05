using System.Collections.Concurrent;

namespace ThinkNet.Infrastructure
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
        public virtual void AddOrUpdatePublishedVersion(DataKey sourceKey, int version)
        { }

        private void AddOrUpdatePublishedVersionToMemory(DataKey sourceKey, int version)
        {
            var sourceTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            _versionCache.GetOrAdd(sourceTypeCode, typeCode => new ConcurrentDictionary<string, int>())
                .AddOrUpdate(sourceKey.SourceId,
                    version,
                    (key, value) => version == value + 1 ? version : value);
        }

        /// <summary>
        /// 获取已发布的溯源聚合版本号
        /// </summary>
        public virtual int GetPublishedVersion(DataKey sourceKey)
        {
            return 0;
        }

        private int GetPublishedVersionFromMemory(DataKey sourceKey)
        {
            ConcurrentDictionary<string, int> dict;
            var sourceTypeCode = sourceKey.GetSourceTypeName().GetHashCode();
            if (_versionCache.TryGetValue(sourceTypeCode, out dict)) {
                int version;
                if (dict.TryGetValue(sourceKey.SourceId, out version)) {
                    return version;
                }
            }     

            return -1;
        }

        #region IEventPublishedVersionStore 成员

        void IEventPublishedVersionStore.AddOrUpdatePublishedVersion(DataKey sourceKey, int version)
        {
            this.AddOrUpdatePublishedVersionToMemory(sourceKey, version);
            this.AddOrUpdatePublishedVersion(sourceKey, version);            
        }

        int IEventPublishedVersionStore.GetPublishedVersion(DataKey sourceKey)
        {
            var version = this.GetPublishedVersionFromMemory(sourceKey);

            if (version < 0) {
                version = this.GetPublishedVersion(sourceKey);
                this.AddOrUpdatePublishedVersionToMemory(sourceKey, version);
            }

            return version;
        }

        #endregion
    }
}
