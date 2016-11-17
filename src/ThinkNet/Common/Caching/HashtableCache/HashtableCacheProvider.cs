using System.Collections.Generic;

namespace ThinkNet.Common.Caching
{
    /// <summary>
    /// .Net Hashtable
    /// </summary>
    public class HashtableCacheProvider : ICacheProvider
    {
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
