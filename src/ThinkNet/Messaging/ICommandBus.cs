using System.Collections.Generic;
using System.Threading.Tasks;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的是命令总线
    /// </summary>
    public interface ICommandBus
    {
        /// <summary>
        /// 异步发送命令并返回结果
        /// </summary>
        Task<CommandResult> SendAsync(ICommand command, CommandReplyType commandReplyType);

        /// <summary>
        /// 异步发送命令
        /// </summary>
        void Send(ICommand command);

        /// <summary>
        /// 异步发送一组命令
        /// </summary>
        void Send(IEnumerable<ICommand> commands);
    }
}
