using System;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 执行消息的调度程序
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// 执行消息结果。
        /// </summary>
        void Execute(IMessage message, out TimeSpan executionTime);
    }
}
