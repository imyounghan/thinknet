using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Reflection;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 提供获取处理消息方法的类
    /// </summary>
    public class HandlerMethodProvider
    {
        /// <summary>
        /// <see cref="HandlerMethodProvider"/> 的一个实例
        /// </summary>
        public static readonly HandlerMethodProvider Instance = new HandlerMethodProvider();


        private readonly ConcurrentDictionary<string, MethodInfo> handleMethodCache;


        private HandlerMethodProvider()
        {
            this.handleMethodCache = new ConcurrentDictionary<string, MethodInfo>();
        }

        /// <summary>
        /// 通过 <param name="contractType" /> 上的泛型类型列表获取 <param name="targetType" /> 的 Handle 方法
        /// </summary>
        public virtual MethodInfo GetMethodInfo(Type targetType, Type contractType)
        {
            List<Type> parameTypes = new List<Type>(contractType.GenericTypeArguments);

            var genericType = contractType.GetGenericTypeDefinition();
            if (genericType == typeof(ICommandHandler<>)) {
                parameTypes.Insert(0, typeof(ICommandContext));
            }
            else if (genericType == typeof(IMessageHandler<>) ||
                genericType == typeof(ICommandHandler<>) ||
                genericType == typeof(IEventHandler<>) ||
                genericType == typeof(IEventHandler<,>) ||
                genericType == typeof(IEventHandler<,,>) ||
                genericType == typeof(IEventHandler<,,,>) ||
                genericType == typeof(IEventHandler<,,,,>)) {
                parameTypes.Insert(0, typeof(SourceMetadata));
            }

            return targetType.GetMethod("Handle", parameTypes.ToArray());
        }
        ///// <summary>
        ///// 通过 <param name="contractType" /> 上的泛型类型列表从缓存中获取 <param name="targetType" /> 的 Handle 方法
        ///// </summary>
        //public MethodInfo GetCachedMethodInfo(Type targetType, Type contractType)
        //{
        //    var contractName = AttributedModelServices.GetContractName(contractType);

        //    return handleMethodCache.GetOrAdd(contractName, delegate(string key) {
        //        var method = this.GetMethodInfo(contractType, targetType);
        //        return method;
        //    });
        //}

        //private MethodInfo GetHandleMethod(Type type, Type[] parameterTypes)
        //{
        //    return type.GetMethod("Handle", parameterTypes);
        //}

        ///// <summary>
        ///// 通过 <param name="contractType" /> 上的泛型类型列表从缓存中获取 <param name="targetType" /> 的 Handle 方法
        ///// </summary>
        //public MethodInfo GetMethodInfo(Type type, IEnumerable<Type> messageTypes)
        //{
        //    List<Type> parameterTypes = new List<Type>(messageTypes);

        //    MethodInfo method = null;
        //    if (parameterTypes.Count == 1) {
        //        if (typeof(ICommand).IsAssignableFrom(parameterTypes[0])) {
        //            parameterTypes.Insert(0, typeof(ICommandContext));
        //            method = GetHandleMethod(type, parameterTypes.ToArray());
        //            if (method == null) {
        //                parameterTypes.RemoveAt(0);
        //            }
        //        }
        //    }
        //    else if (parameterTypes.Count > 1) {
        //        parameterTypes.Insert(0, typeof(SourceDataKey));
        //    }


        //    if (method == null) {
        //        method = GetHandleMethod(type, parameterTypes.ToArray());
        //    }

        //    return method;
        //}


        /// <summary>
        /// 通过 <param name="contractType" /> 上的泛型类型列表从缓存中获取 <param name="targetType" /> 的 Handle 方法
        /// </summary>
        public MethodInfo GetCachedMethodInfo(Type contractType, Func<Type> targetType)
        {
            var contractName = AttributedModelServices.GetContractName(contractType);

            return handleMethodCache.GetOrAdd(contractName, delegate(string key) {
                var method = this.GetMethodInfo(targetType(), contractType);
                return method;
            });
        }
    }
}
