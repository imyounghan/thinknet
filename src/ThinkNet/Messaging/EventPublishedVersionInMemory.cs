namespace ThinkNet.Messaging
{
    using System;
    using System.Collections.Concurrent;

    using ThinkNet.Infrastructure;


    public class EventPublishedVersionInMemory : IEventPublishedVersionStore
    {
        private readonly ConcurrentDictionary<SourceKey, int>[] _versionCaches;

        public EventPublishedVersionInMemory()
            : this(5)
        { }

        protected EventPublishedVersionInMemory(int dictCount)
        {
            this._versionCaches = new ConcurrentDictionary<SourceKey, int>[dictCount];
            for (int index = 0; index < dictCount; index++) {
                _versionCaches[index] = new ConcurrentDictionary<SourceKey, int>();
            }
        }

        public virtual void AddOrUpdatePublishedVersion(SourceKey sourceInfo, int version)
        { }

        public virtual int GetPublishedVersion(SourceKey sourceInfo)
        {
            return 0;
        }

        private int GetPublishedVersionFromMemory(SourceKey sourceKey)
        {
            var dict = _versionCaches[Math.Abs(sourceKey.GetHashCode() % 10)];
            int version;
            if (dict.TryGetValue(sourceKey, out version)) {
                return version;
            }

            return -1;
        }

        private void AddOrUpdatePublishedVersionToMemory(SourceKey sourceKey, int version)
        {
            var dict = _versionCaches[Math.Abs(sourceKey.GetHashCode() % 10)];

            dict.AddOrUpdate(sourceKey,
                version,
                (key, value) => version == value + 1 ? version : value);
        }

        void IEventPublishedVersionStore.AddOrUpdatePublishedVersion(SourceKey sourceInfo, int version)
        {
            this.AddOrUpdatePublishedVersionToMemory(sourceInfo, version);
            this.AddOrUpdatePublishedVersion(sourceInfo, version);
        }

        int IEventPublishedVersionStore.GetPublishedVersion(SourceKey sourceInfo)
        {
            var version = this.GetPublishedVersionFromMemory(sourceInfo);

            if (version < 0) {
                version = this.GetPublishedVersion(sourceInfo);
                this.AddOrUpdatePublishedVersion(sourceInfo, version);
            }

            return version;
        }
    }
}
