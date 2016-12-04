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
    /// <see cref="IQueryService"/> 的实现类
    /// </summary>
    public class QueryService : IQueryService, IQueryResultNotification
    {
        private readonly static TimeSpan WaitTime = TimeSpan.FromSeconds(60);
        private readonly static QueryResult TimeoutResult = new QueryResult(ReturnStatus.Timeout, "Timeout");

        private readonly ConcurrentDictionary<string, TaskCompletionSource<IQueryResult>> _queryTaskDict;
        private readonly IEnvelopeSender _sender;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public QueryService(IEnvelopeSender sender)
        {
            this._queryTaskDict = new ConcurrentDictionary<string, TaskCompletionSource<IQueryResult>>();
            this._sender = sender;
        }

        /// <summary>
        /// 执行查询结果
        /// </summary>
        public IQueryResult Execute(IQuery queryParameter)
        {
            var task = this.ExecuteAsync(queryParameter);
            if(task.Wait(WaitTime)) {
                return task.Result;
            }

            return TimeoutResult;
        }

        /// <summary>
        /// 异步执行查询结果
        /// </summary>
        public Task<IQueryResult> ExecuteAsync(IQuery parameter)
        {
            var taskCompletionSource = new TaskCompletionSource<IQueryResult>();
            if(!_queryTaskDict.TryAdd(parameter.Id, taskCompletionSource)) {
                taskCompletionSource.TrySetException(new Exception("Try add TaskCompletionSource failed."));
                return taskCompletionSource.Task;
            }

            this.SendAsync(parameter).ContinueWith(task => {
                if(task.Status == TaskStatus.Faulted) {
                    this.Notify(parameter.Id, new QueryResult(ReturnStatus.Failed, task.Exception.Message));
                }
            });

            return taskCompletionSource.Task;
        }

        private Task SendAsync(IQuery parameter)
        {
            if(_queryTaskDict.Count > ConfigurationSetting.Current.MaxRequests)
                throw new ThinkNetException("server is busy.");

            var envelope = new Envelope(parameter);
            envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.QueryKind;
            envelope.Metadata[StandardMetadata.SourceId] = parameter.Id;
            var attribute = parameter.GetType().GetCustomAttribute<DataContractAttribute>(false);
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

        #region IQueryResultNotification 成员
        /// <summary>
        /// 通知查询结果
        /// </summary>
        public void Notify(string queryId, IQueryResult queryResult)
        {
            if(_queryTaskDict.Count == 0)
                return;

            TaskCompletionSource<IQueryResult> taskCompletionSource;
            if(_queryTaskDict.TryRemove(queryId, out taskCompletionSource)) {
                taskCompletionSource.TrySetResult(queryResult);
            }
        }

        #endregion
    }
}
