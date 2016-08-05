using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;


namespace ThinkNet.Messaging
{
    /// <summary>
    /// <see cref="ICommandResultManager"/> 的实现
    /// </summary>
    public class CommandResultManager : ICommandResultManager
    {
        private readonly ConcurrentDictionary<string, CommandTaskCompletionSource> _commandTaskDict;
        private readonly ICommandBus _commandBus;

        /// <summary>
        /// default constructor.
        /// </summary>
        public CommandResultManager()
        {
            this._commandTaskDict = new ConcurrentDictionary<string, CommandTaskCompletionSource>();
        }

        protected CommandResultManager(ICommandBus commandBus)
            : this()
        {
            this._commandBus = commandBus;
        }

        /// <summary>
        /// 注册一个命令
        /// </summary>
        public Task<CommandResult> RegisterCommand(ICommand command, CommandResultType commandReplyType)
        {
            return this.RegisterCommand(command, commandReplyType, _commandBus.Send);
        }

        /// <summary>
        /// 注册一个命令
        /// </summary>
        public Task<CommandResult> RegisterCommand(ICommand command, CommandResultType commandReplyType, Action<ICommand> commandAction)
        {
            var commandTaskCompletionSource = _commandTaskDict.GetOrAdd(command.Id, key => new CommandTaskCompletionSource(commandReplyType));

            commandAction(command);

            return commandTaskCompletionSource.TaskCompletionSource.Task;
        }

        /// <summary>
        /// 通知命令处理完成
        /// </summary>
        protected void NotifyCommandCompleted(string commandId, CommandStatus status, Exception exception)
        {
            var commandResult = new CommandResult(status, commandId, exception);
            this.NotifyCommandCompleted(commandResult);
        }

        /// <summary>
        /// 通知命令处理完成
        /// </summary>
        protected void NotifyCommandCompleted(CommandResult commandResult)
        {
            if (_commandTaskDict.Count == 0)
                return;

            CommandTaskCompletionSource commandTaskCompletionSource;
            if (_commandTaskDict.TryRemove(commandResult.CommandId, out commandTaskCompletionSource)) {
                commandTaskCompletionSource.TaskCompletionSource.TrySetResult(commandResult);
            }
        }

        /// <summary>
        /// 通知命令已处理
        /// </summary>
        protected void NotifyCommandExecuted(string commandId, CommandStatus status, Exception exception)
        {
            var commandResult = new CommandResult(status, commandId, exception);
            this.NotifyCommandExecuted(commandResult);
        }

        /// <summary>
        /// 通知命令已处理
        /// </summary>
        protected void NotifyCommandExecuted(CommandResult commandResult)
        {
            if (_commandTaskDict.Count == 0)
                return;

            CommandTaskCompletionSource commandTaskCompletionSource;
            bool completed = false;
            if (_commandTaskDict.TryGetValue(commandResult.CommandId, out commandTaskCompletionSource)) {
                if (commandTaskCompletionSource.CommandReplyType == CommandResultType.CommandExecuted) {
                    completed = true;
                }
                else if (commandTaskCompletionSource.CommandReplyType == CommandResultType.DomainEventHandled) {
                    completed = (commandResult.Status == CommandStatus.Failed || commandResult.Status == CommandStatus.NothingChanged);
                }
            }

            if (completed) {
                this.NotifyCommandCompleted(commandResult);
            }
        }

        class CommandTaskCompletionSource
        {
            public CommandTaskCompletionSource(CommandResultType commandReplyType)
            {
                this.CommandReplyType = commandReplyType;
                this.TaskCompletionSource = new TaskCompletionSource<CommandResult>();
            }

            public TaskCompletionSource<CommandResult> TaskCompletionSource { get; set; }
            public CommandResultType CommandReplyType { get; set; }
        }
    }
}
