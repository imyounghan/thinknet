using System.Collections.Concurrent;
using ThinkLib;

namespace ThinkNet.Domain.EventSourcing
{
    public class PublishedVersionInMemory : IPublishedVersionStore
    {
        private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, int>> _versionCache;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public PublishedVersionInMemory()
        {
            this._versionCache = new ConcurrentDictionary<int, ConcurrentDictionary<string, int>>();
        }

        /// <summary>
        /// 添加或更新溯源聚合的版本号
        /// </summary>
        public virtual void AddOrUpdatePublishedVersion(DataKey sourceKey, int version)
        {
            var sourceTypeCode = sourceKey.GetSourceTypeName().GetHashCode();

            _versionCache.GetOrAdd(sourceTypeCode, () => new ConcurrentDictionary<string, int>())
                .AddOrUpdate(sourceKey.Id,
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
                if (dict.TryGetValue(sourceKey.Id, out version)) {
                    return version;
                }
            }     

            return -1;
        }

        #region IPublishedVersionStore 成员

        int IPublishedVersionStore.GetPublishedVersion(DataKey sourceKey)
        {
            var version = this.GetPublishedVersionFromMemory(sourceKey);

            if (version < 0) {
                version = this.GetPublishedVersion(sourceKey);
                this.AddOrUpdatePublishedVersion(sourceKey, version);
            }

            return version;
        }

        #endregion
    }
}
