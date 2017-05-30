using System.Collections;
using System.Collections.Generic;

namespace ThinkNet.Infrastructure.Caching
{
    /// <summary>
    /// .Net Hashtable
    /// </summary>
    public class HashtableCacheProvider : ICacheProvider
    {
        class HashtableCache : ICache
        {
            private Hashtable hashtable;

            public HashtableCache(string regionName)
            {
                this.RegionName = regionName;
                this.hashtable = Hashtable.Synchronized(new Hashtable());
            }

            public object Get(string key)
            {
                return hashtable[key];
            }

            public void Put(string key, object value)
            {
                hashtable[key] = value;
            }

            public void Remove(string key)
            {
                hashtable.Remove(key);
            }

            public void Clear()
            {
                hashtable.Clear();
            }

            public void Destroy()
            {
                this.Clear();
                hashtable = null;
            }

            public string RegionName
            {
                get;
                private set;
            }
        }

        /// <summary>
        /// 创建区域缓存
        /// </summary>
        public ICache BuildCache(string regionName, IDictionary<string, string> properties)
        {
            return this.BuildCache(regionName);
        }

        /// <summary>
        /// 创建区域缓存
        /// </summary>
        public ICache BuildCache(string regionName)
        {
            return new HashtableCache(regionName);
        }
    }
}
