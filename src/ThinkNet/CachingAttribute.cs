using System;

namespace ThinkNet
{
    /// <summary>
    /// 表示由此特性所描述的方法，能够获得框架所提供的缓存功能。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class CachingAttribute : CacheRegionAttribute
    {
        /// <summary>
        /// 表示用于缓存特性的缓存方式。
        /// </summary>
        public enum CachingMethod
        {
            /// <summary>
            /// 表示需要从缓存中获取对象。如果缓存中不存在所需的对象，系统则会调用实际的方法获取对象，然后将获得的结果添加到缓存中。
            /// </summary>
            Get,
            /// <summary>
            /// 表示需要将对象存入缓存。此方式会调用实际方法以获取对象，然后将获得的结果添加到缓存中，并直接返回方法的调用结果。
            /// </summary>
            Put,
            /// <summary>
            /// 表示需要将对象从缓存中移除。
            /// </summary>
            Remove
        }

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
