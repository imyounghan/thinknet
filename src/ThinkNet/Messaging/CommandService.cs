

namespace ThinkNet.Messaging
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using ThinkNet.Infrastructure;

    public class CommandService : ICommandService, ISendReplyService
    {

        private readonly static TimeSpan WaitTime = TimeSpan.FromSeconds(10000);

        private static readonly CommandResult TimeoutResult = new CommandResult() {
            Status = ExecutionStatus.Timeout,
            ErrorCode = "-1",
            ErrorMessage = "Operation is timeout."
        };
        private readonly static CommandResult BusyResult = new CommandResult() {
            Status = ExecutionStatus.Failed,
            ErrorCode = "-1",
            ErrorMessage = "Server is busy."
        };

        private static readonly CommandResult SuccessResult = new CommandResult() { Status = ExecutionStatus.Success };


        private readonly ConcurrentDictionary<string, TaskCompletionSource<ICommandResult>> _commandTaskDict;
        //private readonly Dictionary<string, Type> _commandTypeMaps;

        private readonly ICommandBus commandBus;
        private readonly ITextSerializer serializer;
        private readonly ILogger logger;

        public CommandService(ICommandBus commandBus, ITextSerializer serializer, ILoggerFactory loggerFactory)
        {
            this.commandBus = commandBus;
            this.serializer = serializer;
            this.logger = loggerFactory.GetDefault();

            this._commandTaskDict = new ConcurrentDictionary<string, TaskCompletionSource<ICommandResult>>();
        }


        #region ICommandService 成员

        public ICommandResult Send(ICommand command)
        {
            try
            {
                commandBus.Send(command);

                return SuccessResult;
            }
            catch (Exception ex)
            {
                if (this.logger.IsErrorEnabled) {
                    logger.Error(ex);
                }

                return new CommandResult() {
                    Status = ExecutionStatus.Failed,
                    ErrorCode = "-1",
                    ErrorMessage = "Send Command failed."
                };     
            }
        }

        public ICommandResult Execute(ICommand command)
        {
            if(_commandTaskDict.Count > 2000) {

                return BusyResult;
            }

            string traceId = ObjectId.GenerateNewStringId();
            var commandTask = _commandTaskDict.GetOrAdd(traceId, () => new TaskCompletionSource<ICommandResult>()).Task;
            

            try {
                commandBus.Send(command, new TraceInfo(traceId, ""));
            }
            catch(Exception ex) {
                _commandTaskDict.Remove(traceId);
                if (this.logger.IsErrorEnabled) {
                    logger.Error(ex);
                }
                return new CommandResult() {
                    Status = ExecutionStatus.Failed,
                    ErrorCode = "-1",
                    ErrorMessage = "Send Command failed."
                };
            }

            if(!commandTask.Wait(WaitTime)) {
                return TimeoutResult;
            }

            return commandTask.Result;
        }

        public void SendReply(IReplyResult replyResult, string replyAddress)
        {
            var commandResult = replyResult as CommandResult;
            if (commandResult != null) {
                TaskCompletionSource<ICommandResult> task;
                if (_commandTaskDict.TryRemove(commandResult.TraceId, out task)) {
                    task.TrySetResult(commandResult);
                }
            }
        }

        #endregion
    }
}
