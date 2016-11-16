using System;
using System.Reflection;

namespace ThinkNet.Messaging.Handling
{
    public interface IProxyHandler
    {
        void Handle(params object[] args);

        ///// <summary>
        ///// 消息类型
        ///// </summary>
        //Type MessageType { get; }
        /// <summary>
        /// 表示处理器的接口类型。
        /// </summary>
        /// <remarks>
        /// 如IMessageHandler<>, ICommandHandler<>,, IEventHandler<>
        /// </remarks>
        //Type ContractType { get; }
        /// <summary>
        /// 表示处理器的实现类型
        /// </summary>
        //Type TargetType { get; }

        //IHandler GetTargetHandler();

        MethodInfo Method { get; }

        IHandler ReflectedHandler { get; }
    }
}
