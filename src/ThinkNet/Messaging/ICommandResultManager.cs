using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ThinkNet.Common;

namespace ThinkNet.Messaging
{
    [RequiredComponent(typeof(DefaultCommandResultManager))]
    /// <summary>
    /// 表示命令结果管理器的接口
    /// </summary>
    public interface ICommandResultManager
    {
        /// <summary>
        /// 注册当前命令到管理器
        /// </summary>
        Task<CommandResult> RegisterCommand(ICommand command, CommandReplyType commandReplyType);

        /// <summary>
        /// 通知命令已完成
        /// </summary>
        void NotifyCommandCompleted(string commandId, CommandStatus status, Exception exception);

        /// <summary>
        /// 通知命令已执行
        /// </summary>
        void NotifyCommandExecuted(string commandId, CommandStatus status, Exception exception);
    }


    internal class DefaultCommandResultManager : ICommandResultManager
    {
        private readonly ConcurrentDictionary<string, CommandTaskCompletionSource> _commandTaskDict;

        /// <summary>
        /// default constructor.
        /// </summary>
        public DefaultCommandResultManager()
        {
            this._commandTaskDict = new ConcurrentDictionary<string, CommandTaskCompletionSource>();
        }


        /// <summary>
        /// 注册一个命令
        /// </summary>
        public Task<CommandResult> RegisterCommand(ICommand command, CommandReplyType commandReplyType)
        {
            var commandTaskCompletionSource = _commandTaskDict.GetOrAdd(command.Id, key => new CommandTaskCompletionSource(commandReplyType));

            return commandTaskCompletionSource.TaskCompletionSource.Task;
        }

        /// <summary>
        /// 通知命令处理完成
        /// </summary>
        public void NotifyCommandCompleted(string commandId, CommandStatus status, Exception exception)
        {
            CommandTaskCompletionSource commandTaskCompletionSource;
            if(_commandTaskDict.TryRemove(commandId, out commandTaskCompletionSource)) {

                CommandResult commandResult;
                if(exception == null) {
                    commandResult = new CommandResult(status, commandId, string.Empty, string.Empty);
                }
                else {
                    commandResult = new CommandResult(status, commandId, exception);
                }
                commandTaskCompletionSource.TaskCompletionSource.TrySetResult(commandResult);
            }
        }

        /// <summary>
        /// 通知命令已处理
        /// </summary>
        public void NotifyCommandExecuted(string commandId, CommandStatus status, Exception exception)
        {
            CommandTaskCompletionSource commandTaskCompletionSource;
            bool completed = false;
            if(_commandTaskDict.TryGetValue(commandId, out commandTaskCompletionSource)) {
                if (commandTaskCompletionSource.CommandReplyType == CommandReplyType.CommandExecuted) {
                    completed = true;
                }
                else if (commandTaskCompletionSource.CommandReplyType == CommandReplyType.DomainEventHandled) {
                    completed = (status == CommandStatus.Failed || status == CommandStatus.NothingChanged);
                }
            }

            if(completed) {
                //var commandResult = new CommandResult(commandId, commandStatus, exception);
                //commandTaskCompletionSource.TaskCompletionSource.TrySetResult(commandResult);
                //_commandTaskDict.TryRemove(commandId, out commandTaskCompletionSource);
                this.NotifyCommandCompleted(commandId, status, exception);
            }
        }

        class CommandTaskCompletionSource
        {
            public CommandTaskCompletionSource(CommandReplyType commandReplyType)
            {
                this.CommandReplyType = commandReplyType;
                this.TaskCompletionSource = new TaskCompletionSource<CommandResult>();
            }

            public TaskCompletionSource<CommandResult> TaskCompletionSource { get; set; }
            public CommandReplyType CommandReplyType { get; set; }
        }
    }
}
