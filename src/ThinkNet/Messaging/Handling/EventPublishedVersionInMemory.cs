using System.Collections.Concurrent;
using ThinkLib;
using ThinkNet.Domain.EventSourcing;

namespace ThinkNet.Messaging.Handling
{
    public class EventPublishedVersionInMemory : IEventPublishedVersionStore
    {
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, int>> _versionCache;

        /// <summary>
        /// Default Constructor.
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
                if (dict.TryGetValue(sourceKey.Id, out version)) {
                    return version;
                }
            }     

            return -1;
        }

        public virtual void AddOrUpdatePublishedVersionToMemory(DataKey sourceKey, int version)
        {
            var sourceTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            _versionCache.GetOrAdd(sourceTypeCode, () => new ConcurrentDictionary<string, int>())
                .AddOrUpdate(sourceKey.Id,
                    version,
                    (key, value) => version == value + 1 ? version : value);
        }

        #region IPublishedVersionStore 成员

        int IEventPublishedVersionStore.GetPublishedVersion(DataKey sourceKey)
        {
            var version = this.GetPublishedVersionFromMemory(sourceKey);

            if (version < 0) {
                version = this.GetPublishedVersion(sourceKey);
                this.AddOrUpdatePublishedVersion(sourceKey, version);
            }

            return version;
        }

        void IEventPublishedVersionStore.AddOrUpdatePublishedVersion(DataKey sourceKey, int version)
        {
            this.AddOrUpdatePublishedVersionToMemory(sourceKey, version);
            this.AddOrUpdatePublishedVersion(sourceKey, version);
        }

        #endregion
    }
}
