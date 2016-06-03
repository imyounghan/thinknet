using System;
using System.Threading.Tasks;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示命令结果管理器的接口
    /// </summary>
    public interface ICommandResultManager
    {
        /// <summary>
        /// 注册当前命令到管理器
        /// </summary>
        Task<CommandResult> RegisterCommand(ICommand command, CommandReplyType commandReplyType = CommandReplyType.CommandExecuted);

        /// <summary>
        /// 注册当前命令到管理器
        /// </summary>
        Task<CommandResult> RegisterCommand(ICommand command, CommandReplyType commandReplyType, Action<ICommand> commandAction);

        ///// <summary>
        ///// 通知命令已完成
        ///// </summary>
        //void NotifyCommandCompleted(string commandId, CommandStatus status, Exception exception = null);

        ///// <summary>
        ///// 通知命令已执行
        ///// </summary>
        //void NotifyCommandExecuted(string commandId, CommandStatus status, Exception exception = null);
    }
}
