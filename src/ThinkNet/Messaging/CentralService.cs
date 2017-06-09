

namespace ThinkNet.Messaging
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    using ThinkNet.Infrastructure;

    public class CentralService : ICommandService, IQueryService, ISendReplyService
    {

        private readonly static TimeSpan WaitTime = TimeSpan.FromSeconds(10000);

        private static readonly CommandResult TimeoutCommandResult = new CommandResult(null, ExecutionStatus.Timeout, "Operation is timeout.");
        private readonly static CommandResult SentFailedCommandResult = new CommandResult(null, ExecutionStatus.Failed, "Send to bus failed.");
        private readonly static CommandResult BusyCommandResult = new CommandResult(null, ExecutionStatus.Failed, "Server is busy.");
        private static readonly CommandResult SuccessCommandResult = new CommandResult(null);

        private static readonly QueryResult TimeoutQueryResult = new QueryResult(null, ExecutionStatus.Timeout, "Operation is timeout.");
        private readonly static QueryResult SentFailedQueryResult = new QueryResult(null, ExecutionStatus.Failed, "Send to bus failed.");
        private readonly static QueryResult BusyQueryResult = new QueryResult(null, ExecutionStatus.Failed, "Server is busy.");


        private readonly ConcurrentDictionary<string, TaskCompletionSource<ICommandResult>> _commandTaskDict;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<IQueryResult>> _queryTaskDict;
        //private readonly Dictionary<string, Type> _commandTypeMaps;

        private readonly ICommandBus commandBus;
        private readonly IQueryBus queryBus; 
        private readonly ILogger logger;

        public CentralService(ICommandBus commandBus, IQueryBus queryBus, ILoggerFactory loggerFactory)
        {
            this.commandBus = commandBus;
            this.queryBus = queryBus;
            this.logger = loggerFactory.GetDefault();

            this._commandTaskDict = new ConcurrentDictionary<string, TaskCompletionSource<ICommandResult>>();
            this._queryTaskDict = new ConcurrentDictionary<string, TaskCompletionSource<IQueryResult>>();
        }


        #region ICommandService 成员

        public ICommandResult Send(ICommand command)
        {
            try
            {
                commandBus.Send(command);

                return SuccessCommandResult;
            }
            catch (Exception ex)
            {
                if (this.logger.IsErrorEnabled) {
                    logger.Error(ex);
                }

                return SentFailedCommandResult;
            }
        }

        public ICommandResult Execute(ICommand command)
        {
            if(_commandTaskDict.Count > 2000) {

                return BusyCommandResult;
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
                return TimeoutCommandResult;
            }

            return commandTask.Result;
        }

        #endregion

        public void SendReply(IReplyResult replyResult, string replyAddress)
        {
            var commandResult = replyResult as CommandResult;
            if (commandResult != null) {
                TaskCompletionSource<ICommandResult> task;
                if (_commandTaskDict.TryRemove(commandResult.TraceId, out task)) {
                    task.TrySetResult(commandResult);
                }
                return;
            }

            var queryResult = replyResult as QueryResult;
            if(queryResult != null) {
                TaskCompletionSource<IQueryResult> task;
                if(_queryTaskDict.TryRemove(queryResult.TraceId, out task)) {
                    task.TrySetResult(queryResult);
                }
            }
        }

        #region IQueryService 成员

        public IQueryResult<T> Execute<T>(IQuery query)
        {
            var queryResult = this.Execute(query);

            return new QueryResult<T>(queryResult);
        }


        public IQueryResult Execute(IQuery query)
        {
            if(_commandTaskDict.Count > 2000)
            {
                return BusyQueryResult;
            }

            string traceId = ObjectId.GenerateNewStringId();
            var queryTask = _queryTaskDict.GetOrAdd(traceId, () => new TaskCompletionSource<IQueryResult>()).Task;


            try {
                queryBus.Send(query, new TraceInfo(traceId, ""));
            }
            catch(Exception ex) {
                _queryTaskDict.Remove(traceId);
                if(this.logger.IsErrorEnabled) {
                    logger.Error(ex);
                }
                return SentFailedQueryResult;
            }

            if(!queryTask.Wait(WaitTime)) {
                return TimeoutQueryResult;
            }

            return queryTask.Result;
        }

        #endregion
    }
}
