using System;
using System.Collections.Concurrent;
using ThinkNet.Domain.EventSourcing;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// <see cref="IEventPublishedVersionStore"/> 的本地内存存储
    /// </summary>
    public class EventPublishedVersionInMemory : IEventPublishedVersionStore
    {
        private readonly ConcurrentDictionary<string, int>[] _versionCache;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public EventPublishedVersionInMemory()
        {
            this._versionCache = new ConcurrentDictionary<string, int>[10];
            for(int index = 0; index < 10; index++) {
                _versionCache[index] = new ConcurrentDictionary<string, int>();
            }
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
            var dict = _versionCache[Math.Abs(sourceKey.GetHashCode() % 10) - 1];
            int version;
            if(dict.TryGetValue(sourceKey.Id, out version)) {
                return version;
            }

            return -1;
        }

        /// <summary>
        /// 添加或更新版本号到内存中
        /// </summary>
        protected void AddOrUpdatePublishedVersionToMemory(DataKey sourceKey, int version)
        {
            var dict = _versionCache[Math.Abs(sourceKey.GetHashCode() % 10) - 1];

            dict.AddOrUpdate(sourceKey.Id,
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
