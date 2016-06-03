using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.Practices.Unity.InterceptionExtension;
using ThinkLib.Caching;
using ThinkLib.Common;

namespace ThinkNet.Configurations
{
    /// <summary>
    /// 表示用于方法缓存功能的拦截行为。
    /// </summary>
    public class CachingBehavior : IInterceptionBehavior
    {
        public virtual IEnumerable<Type> GetRequiredInterfaces()
        {
            return Type.EmptyTypes;
        }

        public virtual IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptionBehaviorDelegate getNext)
        {
            var method = input.MethodBase;
            if (method.IsDefined<CachingAttribute>(false)) {
                var cachingAttribute = method.GetAttribute<CachingAttribute>(false);

                string cacheKey = cachingAttribute.CacheKey;
                if (string.IsNullOrEmpty(cacheKey)) {
                    cacheKey = CreateCacheKey(input);
                }

                string cacheRegion = cachingAttribute.CacheRegion;
                if (string.IsNullOrEmpty(cacheRegion)) {
                    cacheRegion = "ThinkCache";
                }
                var cache = CacheManager.GetCache(cacheRegion);

                switch (cachingAttribute.Method) {
                    case CachingMethod.Get:
                        if (TargetMethodReturnsVoid(input)) {
                            return getNext()(input, getNext);
                        }

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

                    case CachingMethod.Put:
                        if (TargetMethodReturnsVoid(input)) {
                            return getNext()(input, getNext);
                        }

                        IMethodReturn methodReturn = getNext().Invoke(input, getNext);
                        CacheManager.GetCache(cacheRegion).Put(cacheKey, methodReturn.ReturnValue);

                        return methodReturn;
                    case CachingMethod.Remove:
                        foreach (var region in cachingAttribute.RelatedAreas) {
                            CacheManager.GetCache(region).Clear();
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
