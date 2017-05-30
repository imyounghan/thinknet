using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Caching;

namespace ThinkNet.Infrastructure.Caching
{
    /// <summary>
    /// .Net MemoryCache
    /// </summary>
    public class MemoryCacheProvider : ICacheProvider
    {
        class MemoryCache : ICache
        {
            private readonly System.Runtime.Caching.MemoryCache cache;
            private readonly string regionName;
            private readonly CacheItemPolicy policy;
            private readonly string rootCacheKey;

            private bool rootCacheKeyStored;

            public MemoryCache(string regionName, IDictionary<string, string> properties)
            {
                this.regionName = regionName;
                this.policy = new CacheItemPolicy() {
                    SlidingExpiration = GetExpiration(properties),
                    Priority = GetPriority(properties)
                };

                this.cache = System.Runtime.Caching.MemoryCache.Default;
                this.rootCacheKey = GenerateRootCacheKey();

                this.policy.ChangeMonitors.Add(
                    this.cache.CreateCacheEntryChangeMonitor(new[] { rootCacheKey }, regionName)
                    );
            }

            private string GenerateRootCacheKey()
            {
                return GetCacheKey(Guid.NewGuid().ToString());
            }

            private string GetCacheKey(string key)
            {
                return String.Concat("ThinkCache:", key, "@", key.GetHashCode());
            }

            private static TimeSpan GetExpiration(IDictionary<string, string> props)
            {
                TimeSpan expiration = TimeSpan.FromSeconds(300);
                string expirationString;
                if(props != null && props.TryGetValue("expiration", out expirationString)) {
                    expiration = TimeSpan.FromSeconds(expirationString.ChangeIfError(300));
                }

                return expiration;
            }

            private static CacheItemPriority GetPriority(IDictionary<string, string> props)
            {
                CacheItemPriority result = CacheItemPriority.Default;
                string priorityString;
                if(props != null && props.TryGetValue("priority", out priorityString)) {
                    result = ConvertCacheItemPriorityFromXmlString(priorityString);
                }
                return result;
            }

            private static CacheItemPriority ConvertCacheItemPriorityFromXmlString(string priorityString)
            {
                if(string.IsNullOrEmpty(priorityString)) {
                    return CacheItemPriority.Default;
                }

                var ps = priorityString.Trim().ToLowerInvariant();

                if(ps.IsNumeric()) {
                    if(int.Parse(ps) == 1) {
                        return CacheItemPriority.NotRemovable;
                    }
                }

                switch(ps) {
                    case "notremovable":
                        return CacheItemPriority.NotRemovable;
                    default:
                        return CacheItemPriority.Default;
                }
            }

            private void StoreRootCacheKey()
            {
                rootCacheKeyStored = true;

                cache.Add(
                    rootCacheKey,
                    rootCacheKey,
                    new CacheItemPolicy {
                        Priority = CacheItemPriority.NotRemovable,
                        RemovedCallback = new CacheEntryRemovedCallback(args => {
                            rootCacheKeyStored = false;
                        })
                    });
            }

            #region ICache 成员

            public object Get(string key)
            {
                if(string.IsNullOrWhiteSpace(key))
                    throw new ArgumentNullException("key", "the key is null or empty.");

                string cacheKey = GetCacheKey(key);

                object obj = cache.Get(cacheKey, regionName);
                if(obj == null)
                    return null;

                var de = (DictionaryEntry)obj;
                if(key == de.Key.ToString()) {
                    return de.Value;
                }
                else {
                    return null;
                }
            }

            public void Put(string key, object value)
            {
                if(string.IsNullOrWhiteSpace(key))
                    throw new ArgumentNullException("key", "the key is null or empty.");


                if(!rootCacheKeyStored) {
                    StoreRootCacheKey();
                }

                string cacheKey = GetCacheKey(key);
                if(cache.Contains(key, regionName)) {
                    cache.Set(cacheKey, new DictionaryEntry(key, value), policy, regionName);
                }
                else {
                    cache.Add(cacheKey, new DictionaryEntry(key, value), policy, regionName);
                }
            }

            public void Remove(string key)
            {
                if(string.IsNullOrWhiteSpace(key))
                    throw new ArgumentNullException("key", "the key is null or empty.");

                string cacheKey = GetCacheKey(key);

                if(cache.Contains(key, regionName)) {
                    cache.Remove(cacheKey, regionName);
                }
            }

            public void Clear()
            {
                cache.Remove(rootCacheKey, regionName);
            }

            public string RegionName
            {
                get { return this.regionName; }
            }

            #endregion

        }

        private readonly Dictionary<string, ICache> caches;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MemoryCacheProvider()
            : this(CacheConfigurationSectionHandler.SectionName)
        { }

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public MemoryCacheProvider(string sectionKey)
        {
            this.caches = (ConfigurationManager.GetSection(sectionKey) as IEnumerable<CacheConfiguration>)
                .Safe()
                .ToDictionary(config => config.Region, config => BuildCache(config.Region, config.Properties));
        }

        /// <summary>
        /// 创建区域缓存
        /// </summary>
        public ICache BuildCache(string regionName, IDictionary<string, string> properties)
        {
            ICache result;
            if (caches != null && caches.TryGetValue(regionName, out result)) {
                return result;
            }

            return new MemoryCache(regionName, properties);
        }

        /// <summary>
        /// 创建区域缓存
        /// </summary>
        public ICache BuildCache(string regionName)
        {
            return this.BuildCache(regionName, null);
        }
    }
}
