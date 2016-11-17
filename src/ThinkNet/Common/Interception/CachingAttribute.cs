using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using ThinkNet.Common.Caching;
using ThinkNet.Common.Composition;

namespace ThinkNet.Common.Interception
{
    /// <summary>
    /// 表示此特性能够获得框架所提供的缓存功能。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
    public class CachingAttribute : InterceptorAttribute
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

        /// <summary>
        /// 创建一个可用于缓存的拦截器
        /// </summary>
        public override IInterceptor CreateInterceptor(IObjectContainer container)
        {
            var cacheProvider = container.Resolve<ICacheProvider>();

            return new CachingInterceptor(this, cacheProvider);
        }

        class CachingInterceptor : IInterceptor
        {
            private readonly CachingAttribute _cachingAttribute;
            
            private readonly ICacheProvider _cacheProvider;
            private readonly static ConcurrentDictionary<string, ICache> _caches = new ConcurrentDictionary<string,ICache>();

            public CachingInterceptor(CachingAttribute cachingAttribute, ICacheProvider cacheProvider)
            {
                this._cachingAttribute = cachingAttribute;
                this._cacheProvider = cacheProvider;
            }

            private ICache GetOrBuildCache(string region)
            {
                return _caches.GetOrAdd(region, _cacheProvider.BuildCache);
            }

            public IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptorDelegate getNext)
            {
                string cacheRegion = GetCacheRegion(input.MethodBase);
                string cacheKey = _cachingAttribute.CacheKey;
                if (string.IsNullOrEmpty(cacheKey)) {
                    cacheKey = CreateCacheKey(input);
                }

                switch (_cachingAttribute.Method) {
                    case CachingAttribute.CachingMethod.Get:
                        if (TargetMethodReturnsVoid(input)) {
                            return getNext()(input, getNext);
                        }

                        var cache = this.GetOrBuildCache(cacheRegion);

                        object cachedResult = cache.Get(cacheKey);
                        if (cachedResult == null) {
                            IMethodReturn realReturn = getNext()(input, getNext);
                            cache.Put(cacheKey, realReturn.ReturnValue);

                            return realReturn;
                        }
                        else {
                            IMethodReturn cachedReturn = input.CreateMethodReturn(cachedResult, input.Arguments);
                            return cachedReturn;
                        }

                    case CachingAttribute.CachingMethod.Put:
                        if (TargetMethodReturnsVoid(input)) {
                            return getNext()(input, getNext);
                        }

                        IMethodReturn methodReturn = getNext().Invoke(input, getNext);
                        this.GetOrBuildCache(cacheRegion).Put(cacheKey, methodReturn.ReturnValue);

                        return methodReturn;
                    case CachingAttribute.CachingMethod.Remove:
                        foreach (var region in _cachingAttribute.RelatedAreas) {
                            this.GetOrBuildCache(region).Clear();
                        }
                        return getNext().Invoke(input, getNext);
                }

                return getNext().Invoke(input, getNext);
            }


            static string GetCacheRegion(MethodBase method)
            {
                var cachingAttribute = method.GetAttribute<CacheRegionAttribute>(false);
                if (cachingAttribute == null) {
                    cachingAttribute = method.DeclaringType.GetAttribute<CacheRegionAttribute>(false);
                }

                if (cachingAttribute == null) {
                    return "ThinkCache";
                }

                return cachingAttribute.CacheRegion;
            }

            static bool TargetMethodReturnsVoid(IMethodInvocation input)
            {
                MethodInfo targetMethod = input.MethodBase as MethodInfo;
                return targetMethod != null && targetMethod.ReturnType == typeof(void);
            }

            static string CreateCacheKey(IMethodInvocation input)
            {
                StringBuilder sb = new StringBuilder();
                var method = input.MethodBase;

                //sb.AppendFormat("{0}:", method.Name);
                if (method.DeclaringType != null) {
                    sb.Append(method.DeclaringType.FullName).Append(":");
                    //sb.Append(method.DeclaringType.FullName);
                    //sb.Append(':');
                }
                sb.Append(method.Name);

                foreach (object param in input.Inputs) {
                    if (param != null) {
                        sb.Append(':').Append(param.ToString());
                    }
                }

                return sb.ToString();
            }
        }
    }
}
