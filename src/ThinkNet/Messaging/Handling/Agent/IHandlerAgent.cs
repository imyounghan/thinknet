using System;
using System.Reflection;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 继承该接口的是一个消息处理器的代码
    /// </summary>
    public interface IHandlerAgent
    {
        /// <summary>
        /// 处理消息
        /// </summary>
        void Handle(params object[] args);

        ///// <summary>
        ///// 表示处理器的反射方法
        ///// </summary>
        //MethodInfo ReflectedMethod { get; }

        ///// <summary>
        ///// 表示处理器的实例
        ///// </summary>
        //object HandlerInstance { get; }

        object GetInnerHandler();
    }
}
