using System.Collections.Generic;

namespace ThinkNet.Caching
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
            return new HashtableCache(regionName);
        }

    }
}
