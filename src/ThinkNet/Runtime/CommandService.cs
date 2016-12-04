using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ThinkNet.Contracts;
using ThinkNet.Messaging;
using ThinkNet.Runtime.Routing;

namespace ThinkNet.Runtime
{
    /// <summary>
    /// <see cref="ICommandService"/> 的实现类
    /// </summary>
    public class CommandService : ICommandService, ICommandResultNotification
    {
        private readonly ICommandResult timeoutResult = new CommandResult() {
            CommandReturnType = CommandReturnType.CommandExecuted,
            ErrorCode = "-1",
            Status = ReturnStatus.Timeout
        };

        private readonly TimeSpan timeout = TimeSpan.FromMilliseconds(1000);


        private readonly ConcurrentDictionary<string, CommandTaskCompletionSource> _commandTaskDict;

        private readonly IEnvelopeSender _sender;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public CommandService(IEnvelopeSender sender)
        {
            this._commandTaskDict = new ConcurrentDictionary<string, CommandTaskCompletionSource>();
            this._sender = sender;
        }

        /// <summary>
        /// 发送一个命令
        /// </summary>
        public void Send(ICommand command)
        {
            this.SendAsync(command).Wait();
        }

        /// <summary>
        /// 异步发送一个命令
        /// </summary>
        public Task SendAsync(ICommand command)
        {
            if(_commandTaskDict.Count > ConfigurationSetting.Current.MaxRequests)
                throw new ThinkNetException("server is busy.");

            _commandTaskDict.AddOrUpdate(command.Id, 
                key => new CommandTaskCompletionSource(CommandReturnType.CommandExecuted),
                (key, value) => value);

            var envelope = new Envelope(command);
            envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandKind;
            envelope.Metadata[StandardMetadata.SourceId] = command.Id;
            var attribute = command.GetType().GetCustomAttribute<DataContractAttribute>(false);
            if(attribute != null) {
                bool clearAssemblyName = false;

                if(!string.IsNullOrEmpty(attribute.Namespace)) {
                    envelope.Metadata[StandardMetadata.Namespace] = attribute.Namespace;
                    clearAssemblyName = true;
                }

                if(!string.IsNullOrEmpty(attribute.Name)) {
                    envelope.Metadata[StandardMetadata.TypeName] = attribute.Name;
                    clearAssemblyName = true;
                }

                if(clearAssemblyName)
                    envelope.Metadata.Remove(StandardMetadata.AssemblyName);
            }

            return _sender.SendAsync(envelope);
        }

        /// <summary>
        /// 在规定时间内执行一个命令并返回处理结果
        /// </summary>
        public ICommandResult Execute(ICommand command, CommandReturnType returnType)
        {
            var task = this.ExecuteAsync(command, returnType);
            if(!task.Wait(timeout)) {
                this.Notify(command.Id, timeoutResult, CommandReturnType.CommandExecuted);
            }
            return task.Result;
        }

        /// <summary>
        /// 异步执行一个命令
        /// </summary>
        public Task<ICommandResult> ExecuteAsync(ICommand command, CommandReturnType returnType)
        {
            var commandTaskCompletionSource = _commandTaskDict.GetOrAdd(command.Id, () => new CommandTaskCompletionSource(returnType));
            this.SendAsync(command).ContinueWith(task => {
                if(task.Status == TaskStatus.Faulted) {
                    this.Notify(command.Id, new CommandResult(command.Id, task.Exception), CommandReturnType.CommandExecuted);
                }
            });

            return commandTaskCompletionSource.TaskCompletionSource.Task;
        }

        ///// <summary>
        ///// 通知命令已处理
        ///// </summary>
        //public void NotifyCommandHandled(ICommandResult commandResult)
        //{
        //    if(_commandTaskDict.Count == 0)
        //        return;

        //    CommandTaskCompletionSource commandTaskCompletionSource;
        //    bool completed = false;
        //    if(_commandTaskDict.TryGetValue(commandResult.CommandId, out commandTaskCompletionSource)) {
        //        if(commandTaskCompletionSource.CommandReplyType == CommandReturnType.CommandExecuted) {
        //            completed = true;
        //        }
        //        else if(commandTaskCompletionSource.CommandReplyType == CommandReturnType.DomainEventHandled) {
        //            completed = (commandResult.Status == CommandStatus.Failed || commandResult.Status == CommandStatus.NothingChanged);
        //        }
        //    }

        //    if(completed) {
        //        this.NotifyEventHandled(commandResult);
        //    }
        //}

        ///// <summary>
        ///// 通知由命令产生的领域事件已处理
        ///// </summary>
        //public void NotifyEventHandled(ICommandResult commandResult)
        //{
        //    if(_commandTaskDict.Count == 0)
        //        return;

        //    CommandTaskCompletionSource commandTaskCompletionSource;
        //    if(_commandTaskDict.TryRemove(commandResult.CommandId, out commandTaskCompletionSource)) {
        //        commandTaskCompletionSource.TaskCompletionSource.TrySetResult(commandResult);
        //    }
        //}

        class CommandTaskCompletionSource
        {
            public CommandTaskCompletionSource(CommandReturnType commandReplyType)
            {
                this.CommandReplyType = commandReplyType;
                this.TaskCompletionSource = new TaskCompletionSource<ICommandResult>();
            }

            public TaskCompletionSource<ICommandResult> TaskCompletionSource { get; set; }
            public CommandReturnType CommandReplyType { get; set; }
        }

        #region ICommandResultNotification 成员
        /// <summary>
        /// 通知命令结果
        /// </summary>
        public void Notify(string commandId, ICommandResult commandResult, CommandReturnType returnType)
        {
            if(_commandTaskDict.Count == 0)
                return;

            CommandTaskCompletionSource commandTaskCompletionSource;
            bool completed = false;
            if(_commandTaskDict.TryGetValue(commandId, out commandTaskCompletionSource)) {
               
                if(commandResult.Status != ReturnStatus.Success) {
                    completed = true;
                }
                else {
                    completed = commandTaskCompletionSource.CommandReplyType == returnType;
                }

                //if(commandTaskCompletionSource.CommandReplyType == CommandReturnType.CommandExecuted) {
                //    completed = true;
                //}
                //else if(commandTaskCompletionSource.CommandReplyType == CommandReturnType.DomainEventHandled) {
                //    completed = (commandResult.Status == CommandStatus.Failed || commandResult.Status == CommandStatus.NothingChanged);
                //}
            }

            if(completed) {
                commandTaskCompletionSource.TaskCompletionSource.TrySetResult(commandResult);
                _commandTaskDict.TryRemove(commandId);
            }
        }

        #endregion
    }
}
