using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace ThinkNet.Common.Caching
{
    /// <summary>
    /// .Net MemoryCache
    /// </summary>
    public class MemoryCacheProvider : ICacheProvider
    {
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

        #region ICacheProvider 成员

        public ICache BuildCache(string regionName)
        {
            return this.BuildCache(regionName, null);
        }

        #endregion
    }
}
