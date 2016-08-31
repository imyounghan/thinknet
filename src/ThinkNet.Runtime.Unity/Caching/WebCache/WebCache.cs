using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Caching;

namespace ThinkNet.Caching
{
    internal sealed class WebCache : ICache
    {
        private const string CacheKeyPrefix = "ThinkCache:";

        private readonly Cache cache;
        private readonly string region;
        private readonly TimeSpan expiration;
        private readonly CacheItemPriority priority;
        private readonly string rootCacheKey;

        public WebCache(string region, IDictionary<string, string> properties)
        {
            this.region = region;
            this.expiration = GetExpiration(properties);
            this.priority = GetPriority(properties);

            this.cache = HttpRuntime.Cache;
            this.rootCacheKey = GenerateRootCacheKey();
            StoreRootCacheKey();
        }

        private bool rootCacheKeyStored;

        #region
        private static TimeSpan GetExpiration(IDictionary<string, string> props)
        {
            TimeSpan expiration = TimeSpan.FromSeconds(300);
            string expirationString;
            if (props != null && props.TryGetValue("expiration", out expirationString)) {
                expiration = TimeSpan.FromSeconds(expirationString.Change(300));
            }

            return expiration;
        }

        private static CacheItemPriority GetPriority(IDictionary<string, string> props)
        {
            CacheItemPriority result = CacheItemPriority.Default;
            string priorityString;
            if (props != null && props.TryGetValue("priority", out priorityString)) {
                result = ConvertCacheItemPriorityFromXmlString(priorityString);
            }
            return result;
        }

        private static CacheItemPriority ConvertCacheItemPriorityFromXmlString(string priorityString)
        {
            if (string.IsNullOrEmpty(priorityString)) {
                return CacheItemPriority.Default;
            }

            var ps = priorityString.Trim().ToLowerInvariant();

            if (ps.IsNumeric()) {
                int priorityAsInt = int.Parse(ps);
                if (priorityAsInt >= 1 && priorityAsInt <= 6) {
                    return (CacheItemPriority)priorityAsInt;
                }
            }

            switch (ps) {
                case "abovenormal":
                    return CacheItemPriority.AboveNormal;
                case "belownormal":
                    return CacheItemPriority.BelowNormal;
                case "high":
                    return CacheItemPriority.High;
                case "low":
                    return CacheItemPriority.Low;
                case "normal":
                    return CacheItemPriority.Normal;
                case "notremovable":
                    return CacheItemPriority.NotRemovable;
                default:
                    return CacheItemPriority.Default;
            }
        }

        private string GetCacheKey(string key)
        {
            return String.Concat(CacheKeyPrefix, RegionName, ":", key, "@", key.GetHashCode());
        }

        private string GenerateRootCacheKey()
        {
            return GetCacheKey(Guid.NewGuid().ToString());
        }

        private void RootCacheItemRemoved(string key, object value, CacheItemRemovedReason reason)
        {
            rootCacheKeyStored = false;
        }

        private void StoreRootCacheKey()
        {
            rootCacheKeyStored = true;
            cache.Add(
                rootCacheKey,
                rootCacheKey,
                null,
                System.Web.Caching.Cache.NoAbsoluteExpiration,
                System.Web.Caching.Cache.NoSlidingExpiration,
                CacheItemPriority.Default,
                RootCacheItemRemoved);
        }

        private void RemoveRootCacheKey()
        {
            cache.Remove(rootCacheKey);
        }
        #endregion
        


        #region ICache 成员

        public object Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key", "the key is null or empty.");

            string cacheKey = GetCacheKey(key);

            object obj = cache.Get(cacheKey);
            if (obj == null)
                return null;

            var de = (DictionaryEntry)obj;
            if (key == de.Key.ToString()) {
                return de.Value;
            }
            else {
                return null;
            }
        }

        public void Put(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key", "the key is null or empty.");

            string cacheKey = GetCacheKey(key);
            if (cache[cacheKey] != null) {

                // Remove the key to re-add it again below
                cache.Remove(cacheKey);
            }

            if (!rootCacheKeyStored) {
                StoreRootCacheKey();
            }

            cache.Insert(cacheKey,
                new DictionaryEntry(key, value),
                new CacheDependency(null, new[] { rootCacheKey }),
                Cache.NoAbsoluteExpiration,
                expiration,
                //DateTime.Now.Add(this.Expiration),
                //System.Web.Caching.Cache.NoSlidingExpiration,
                priority,
                null);
        }

        public void Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException("key", "the key is null or empty.");

            string cacheKey = GetCacheKey(key);
            cache.Remove(cacheKey);
        }

        public void Clear()
        {
            RemoveRootCacheKey();
            StoreRootCacheKey();
        }

        public void Destroy()
        {
            Clear();
        }

        public string RegionName
        {
            get { return this.region; }
        }

        #endregion        
    }
}
