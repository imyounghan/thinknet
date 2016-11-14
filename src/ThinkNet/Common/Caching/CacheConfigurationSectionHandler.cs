using System.Collections.Generic;
using System.Configuration;
using System.Xml;


namespace ThinkNet.Common.Caching
{
    /// <summary>
    /// 配置节点的访问
    /// </summary>
    public class CacheConfigurationSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// 配置节点名称
        /// </summary>
        public const string SectionName = "thinkcache-configuration";

        /// <summary>
        /// parse the config section
        /// </summary>
        /// <returns>an array of CacheConfig objects</returns>
        public object Create(object parent, object configContext, XmlNode section)
        {
            var caches = new List<CacheConfiguration>();

            XmlNodeList nodes = section.SelectNodes("cache");

            foreach (XmlNode node in nodes) {
                string region = null;
                string expiration = null;
                string priority = "3";

                XmlAttribute r = node.Attributes["region"];
                XmlAttribute e = node.Attributes["expiration"];
                XmlAttribute p = node.Attributes["priority"];

                if (r != null) {
                    region = r.Value;
                }

                if (e != null) {
                    expiration = e.Value;
                }

                if (p != null) {
                    priority = p.Value;
                }

                if (region != null && expiration != null) {
                    caches.Add(new CacheConfiguration(region, expiration, priority));
                }
            }

            return caches.ToArray();
        }

    }
}
