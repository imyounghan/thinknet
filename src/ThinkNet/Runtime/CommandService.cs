using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ThinkNet.Contracts;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    /// <summary>
    /// <see cref="ICommandService"/> 的抽象实现类
    /// </summary>
    public abstract class CommandService : DisposableObject, ICommandService, ICommandResultNotification, IInitializer
    {
        private readonly static TimeSpan WaitTime = TimeSpan.FromSeconds(ConfigurationSetting.Current.OperationTimeout);
        private readonly static ICommandResult TimeoutResult = new CommandResult(null, "Operation is timeout.", ReturnStatus.Failed);
        private readonly static ICommandResult BusyResult = new CommandResult(null, "Server is busy.");


        private readonly ConcurrentDictionary<string, CommandTaskCompletionSource> _commandTaskDict;
        private readonly Dictionary<string, Type> _typeMaps;
        /// <summary>
        /// Default constructor.
        /// </summary>
        protected CommandService()
        {
            this._commandTaskDict = new ConcurrentDictionary<string, CommandTaskCompletionSource>();
            this._typeMaps = new Dictionary<string, Type>();
        }

        protected bool TryGetCommandType(string typeName, out Type type)
        {
            return _typeMaps.TryGetValue(typeName, out type);
        }

        /// <summary>
        /// 发送一个命令
        /// </summary>
        public virtual void Send(ICommand command)
        {
            this.SendAsync(command).Wait();
        }

        /// <summary>
        /// 异步发送一个命令
        /// </summary>
        public abstract Task SendAsync(ICommand command);

        /// <summary>
        /// 在规定时间内执行一个命令并返回处理结果
        /// </summary>
        public ICommandResult Execute(ICommand command, CommandReturnMode returnMode)
        {
            var task = this.ExecuteAsync(command, returnMode);
            if(!task.Wait(WaitTime)) {
                this.Notify(command.Id, TimeoutResult, CommandReturnMode.CommandExecuted);
            }
            return task.Result;
        }

        /// <summary>
        /// 异步执行一个命令
        /// </summary>
        public Task<ICommandResult> ExecuteAsync(ICommand command, CommandReturnMode returnMode)
        {
            if(_commandTaskDict.Count > ConfigurationSetting.Current.MaxRequests) {
                if(LogManager.Default.IsWarnEnabled) {
                    LogManager.Default.WarnFormat("Command Service is busy, maxrequests({0}).",
                        ConfigurationSetting.Current.MaxRequests);
                }
                return Task.Factory.StartNew(() => BusyResult);
            }


            var commandTaskCompletionSource = _commandTaskDict.GetOrAdd(command.Id, () => new CommandTaskCompletionSource(returnMode));
            this.SendAsync(command).ContinueWith(task => {
                if(task.Status == TaskStatus.Faulted) {
                    this.Notify(command.Id, new CommandResult(command.Id, task.Exception), CommandReturnMode.CommandExecuted);
                }
            });

            return commandTaskCompletionSource.TaskCompletionSource.Task;
        }        

        class CommandTaskCompletionSource
        {
            public CommandTaskCompletionSource(CommandReturnMode returnMode)
            {
                this.CommandReplyMode = returnMode;
                this.TaskCompletionSource = new TaskCompletionSource<ICommandResult>();
            }

            public TaskCompletionSource<ICommandResult> TaskCompletionSource { get; set; }
            public CommandReturnMode CommandReplyMode { get; set; }
        }

        #region ICommandResultNotification 成员
        /// <summary>
        /// 通知命令结果
        /// </summary>
        public void Notify(string commandId, ICommandResult commandResult, CommandReturnMode returnMode)
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
                    completed = commandTaskCompletionSource.CommandReplyMode == returnMode;
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

                //if(LogManager.Default.IsDebugEnabled) {
                //    LogManager.Default.DebugFormat("The Command Execution complete, commandType:{0}, commandId:{1}, status:{2}.",
                //        commandTaskCompletionSource.CommandType.FullName, commandId, commandResult.Status);
                //}
            }
        }

        #endregion

        #region IInitializer 成员

        void IInitializer.Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            assemblies.SelectMany(p => p.GetExportedTypes())
                .Where(delegate(Type type) {
                    return type.IsClass && !type.IsAbstract && typeof(Command).IsAssignableFrom(type);
                }).ForEach(delegate(Type type) {
                    _typeMaps.Add(type.Name, type);
                });
        }

        #endregion
    }
}
