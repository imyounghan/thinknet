using System;
using ThinkLib.Common;

namespace ThinkNet.Configurations
{
    /// <summary>
    /// 表示由此特性所描述的方法，能够获得框架所提供的缓存功能。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class CachingAttribute : CacheRegionAttribute
    {
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CachingAttribute(CachingMethod method)
            : this(method, new string[0])
        { }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CachingAttribute(CachingMethod method, params string[] relatedAreas)
        {
            this.Method = method;
            this.RelatedAreas = relatedAreas;
        }

        /// <summary>
        /// 获取或设置缓存方式。
        /// </summary>
        public CachingMethod Method { get; private set; }

        /// <summary>
        /// 缓存标识
        /// </summary>
        public string CacheKey { get; set; }

        /// <summary>
        /// 获取与当前缓存方式相关的区域名称。注：此参数仅在缓存方式为Remove时起作用。
        /// </summary>
        public string[] RelatedAreas { get; private set; }
    }
}
