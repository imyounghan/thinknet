using System;
using System.Reflection;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 提供获取处理消息方法的接口。
    /// </summary>
    public interface IHandlerMethodProvider
    {
        /// <summary>
        /// 通过 <param name="contractType" /> 上的泛型类型列表获取 <param name="targetType" /> 的 Handle 方法
        /// </summary>
        MethodInfo GetMethodInfo(Type targetType, Type contractType);
        /// <summary>
        /// 通过 <param name="contractType" /> 上的泛型类型列表从缓存中获取 <param name="targetType" /> 的 Handle 方法
        /// </summary>
        MethodInfo GetCachedMethodInfo(Type targetType, Type contractType);        
    } 
}
