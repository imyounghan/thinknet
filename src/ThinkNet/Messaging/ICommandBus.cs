

namespace ThinkNet.Messaging
{
    using System.Collections.Generic;

    /// <summary>
    /// 表示命令总线的接口
    /// </summary>
    public interface ICommandBus : IMessageBus<Command>
    {
        /// <summary>
        /// 发送一个命令。
        /// </summary>
        /// <param name="command">命令</param>
        /// <param name="traceInfo">跟踪信息</param>
        void Send(Command command, TraceInfo traceInfo);

        /// <summary>
        /// 发送一组消息
        /// </summary>
        /// <param name="commands">命令集合</param>
        /// <param name="traceInfo">跟踪信息</param>
        void Send(IEnumerable<Command> commands, TraceInfo traceInfo);
    }
}
