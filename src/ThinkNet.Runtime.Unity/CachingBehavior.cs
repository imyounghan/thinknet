using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Practices.Unity.InterceptionExtension;
using ThinkNet.Caching;

namespace ThinkNet.Configurations
{
    /// <summary>
    /// 表示用于方法缓存功能的拦截行为。
    /// </summary>
    public class CachingBehavior : IInterceptionBehavior
    {
        private static readonly IDictionary<string, string> EmptyDictionary = new Dictionary<string, string>();

        private readonly ICacheProvider _cacheProvider;
        private readonly ConcurrentDictionary<string, ICache> _caches;

        public CachingBehavior(ICacheProvider cacheProvider)
        {
            this._cacheProvider = cacheProvider;
            this._caches = new ConcurrentDictionary<string, ICache>(StringComparer.CurrentCultureIgnoreCase);
        }

        private ICache BuildCache(string regionName)
        {
            return _cacheProvider.BuildCache(regionName, EmptyDictionary);
        }

        private ICache GetCache(string region)
        {
            return _caches.GetOrAdd(region, BuildCache);
        }

        public virtual IEnumerable<Type> GetRequiredInterfaces()
        {
            return Type.EmptyTypes;
        }

        public virtual IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptionBehaviorDelegate getNext)
        {
            var method = input.MethodBase;

            var cachingAttribute = method.GetAttribute<CachingAttribute>(false);
            if (cachingAttribute != null) {
                string cacheKey = cachingAttribute.CacheKey;
                if (string.IsNullOrEmpty(cacheKey)) {
                    cacheKey = CreateCacheKey(input);
                }

                string cacheRegion = cachingAttribute.CacheRegion;
                if (string.IsNullOrEmpty(cacheRegion)) {
                    cacheRegion = "ThinkCache";
                }
                //var cache = CacheManager.GetCache(cacheRegion);

                switch (cachingAttribute.Method) {
                    case CachingAttribute.CachingMethod.Get:
                        if (TargetMethodReturnsVoid(input)) {
                            return getNext()(input, getNext);
                        }
                        var cache = this.GetCache(cacheRegion);

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
                        this.GetCache(cacheRegion).Put(cacheKey, methodReturn.ReturnValue);

                        return methodReturn;
                    case CachingAttribute.CachingMethod.Remove:
                        foreach (var region in cachingAttribute.RelatedAreas) {
                            this.GetCache(region).Clear();
                        }
                        return getNext().Invoke(input, getNext);
                }
            }

            return getNext().Invoke(input, getNext);
        }

        public virtual bool WillExecute
        {
            get { return true; }
        }

        protected static bool TargetMethodReturnsVoid(IMethodInvocation input)
        {
            MethodInfo targetMethod = input.MethodBase as MethodInfo;
            return targetMethod != null && targetMethod.ReturnType == typeof(void);
        }

        internal static string CreateCacheKey(IMethodInvocation input)
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
