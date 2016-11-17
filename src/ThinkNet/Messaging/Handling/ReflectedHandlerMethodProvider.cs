using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;
using ThinkNet.Domain;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// <see cref="IHandlerMethodProvider"/> 的实现类
    /// </summary>
    public class ReflectedHandlerMethodProvider : IHandlerMethodProvider
    {
        private readonly ConcurrentDictionary<string, MethodInfo> handleMethodCache = new ConcurrentDictionary<string, MethodInfo>();



        #region IHandlerMethodProvider 成员

        /// <summary>
        /// 通过 <param name="contractType" /> 上的泛型类型列表获取 <param name="targetType" /> 的 Handle 方法
        /// </summary>
        public virtual MethodInfo GetMethodInfo(Type targetType, Type contractType)
        {
            List<Type> parameTypes = new List<Type>(contractType.GenericTypeArguments);

            if (contractType == typeof(ICommandHandler<>)) {
                parameTypes.Insert(0, typeof(ICommandContext));
            }
            else if (contractType == typeof(IEventHandler<>)) {
                parameTypes.Insert(0, typeof(SourceDataKey));
            }

            return targetType.GetMethod("Handle", parameTypes.ToArray());
        }
        /// <summary>
        /// 通过 <param name="contractType" /> 上的泛型类型列表从缓存中获取 <param name="targetType" /> 的 Handle 方法
        /// </summary>
        public MethodInfo GetCachedMethodInfo(Type targetType, Type contractType)
        {
            var contractName = AttributedModelServices.GetContractName(contractType);

            return handleMethodCache.GetOrAdd(contractName, delegate(string key) {
                var method = this.GetMethodInfo(contractType, targetType);
                return method;
            });
        }

        #endregion
    }
}
