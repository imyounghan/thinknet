using System.Collections.Generic;

namespace ThinkNet.Infrastructure.Caching
{
    /// <summary>
    /// 缓存配置
    /// </summary>
    public class CacheConfiguration
    {
        private readonly Dictionary<string, string> properties;
        private readonly string region;


        /// <summary>
        /// build a configuration
        /// </summary>
        public CacheConfiguration(string region, string expiration, string priority)
        {
            this.region = region;
            this.properties = new Dictionary<string, string> { 
                { "expiration", expiration }, 
                { "priority", priority } 
            };
        }

        /// <summary>
        /// 区域
        /// </summary>
        public string Region
        {
            get { return this.region; }
        }

        /// <summary>
        /// 配置属性
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get { return properties; }
        }
    }
}
