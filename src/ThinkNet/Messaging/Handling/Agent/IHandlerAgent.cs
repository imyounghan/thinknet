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

        /// <summary>
        /// 获取内部的目标处理器
        /// </summary>
        object GetInnerHandler();
    }
}
