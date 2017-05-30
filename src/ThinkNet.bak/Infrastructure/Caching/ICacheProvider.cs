using System.Collections.Generic;

namespace ThinkNet.Infrastructure.Caching
{
    /// <summary>
    /// 缓存提供
    /// </summary>
    public interface ICacheProvider
    {
        /// <summary>
        /// 建造缓存区。
        /// </summary>
        /// <param name="regionName">缓存区域的名称</param>
        ICache BuildCache(string regionName);

        /// <summary>
        /// 建造缓存区。
        /// </summary>
        /// <param name="regionName">缓存区域的名称</param>
        /// <param name="properties">配置项</param>
        ICache BuildCache(string regionName, IDictionary<string, string> properties);
    }
}
