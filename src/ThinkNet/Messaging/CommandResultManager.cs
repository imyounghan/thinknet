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
        protected CommandResultManager()
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
        public Task<CommandResult> RegisterCommand(ICommand command, CommandReplyType commandReplyType)
        {
            return this.RegisterCommand(command, commandReplyType, _commandBus.Send);
        }

        /// <summary>
        /// 注册一个命令
        /// </summary>
        public Task<CommandResult> RegisterCommand(ICommand command, CommandReplyType commandReplyType, Action<ICommand> commandAction)
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
            if (_commandTaskDict.Count == 0)
                return;

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
        protected void NotifyCommandExecuted(string commandId, CommandStatus status, Exception exception)
        {
            if (_commandTaskDict.Count == 0)
                return;

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
