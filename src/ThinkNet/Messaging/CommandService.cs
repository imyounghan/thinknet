using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// <see cref="ICommandService"/> 的抽像实现
    /// </summary>
    public abstract class CommandService : ICommandService
    {
        private readonly ConcurrentDictionary<string, CommandTaskCompletionSource> _commandTaskDict;

        protected CommandService()
        {
            this._commandTaskDict = new ConcurrentDictionary<string, CommandTaskCompletionSource>();
        }

        public virtual void Send(ICommand command)
        {
            this.SendAsync(command).Wait();
        }

        public abstract Task SendAsync(ICommand command);

        //protected abstract Task SendAsync(ICommand command);

        public CommandResult Execute(ICommand command, CommandReturnType returnType)
        {
            return this.ExecuteAsync(command, returnType).Result;
        }

        public CommandResult Execute(ICommand command, CommandReturnType returnType, TimeSpan timeout)
        {
            var task = this.ExecuteAsync(command, returnType);

            if(!task.Wait(timeout)) {
                this.NotifyCommandCompleted(command.Id, CommandStatus.Timeout, new TimeoutException());
            }
            return task.Result;
        }

        /// <summary>
        /// 执行一个命令
        /// </summary>
        public Task<CommandResult> ExecuteAsync(ICommand command, CommandReturnType returnType)
        {
            var commandTaskCompletionSource = _commandTaskDict.GetOrAdd(command.Id, key => new CommandTaskCompletionSource(returnType));
            this.SendAsync(command).ContinueWith(task => {
                if(task.Status == TaskStatus.Faulted) {
                    this.NotifyCommandCompleted(command.Id, CommandStatus.Failed, task.Exception);
                }
            });
            //this.Send(command);

            return commandTaskCompletionSource.TaskCompletionSource.Task;
        }

        void ICommandService.Send(ICommand command)
        {
            if(_commandTaskDict.Count > 1000)
                throw new ThinkNetException("server is busy.");

            this.Send(command);
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
                if (commandTaskCompletionSource.CommandReplyType == CommandReturnType.CommandExecuted) {
                    completed = true;
                }
                else if (commandTaskCompletionSource.CommandReplyType == CommandReturnType.DomainEventHandled) {
                    completed = (commandResult.Status == CommandStatus.Failed || commandResult.Status == CommandStatus.NothingChanged);
                }
            }

            if (completed) {
                this.NotifyCommandCompleted(commandResult);
            }
        }

        class CommandTaskCompletionSource
        {
            public CommandTaskCompletionSource(CommandReturnType commandReplyType)
            {
                this.CommandReplyType = commandReplyType;
                this.TaskCompletionSource = new TaskCompletionSource<CommandResult>();
            }

            public TaskCompletionSource<CommandResult> TaskCompletionSource { get; set; }
            public CommandReturnType CommandReplyType { get; set; }
        }
    }
}
