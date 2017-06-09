

namespace ThinkNet.Messaging
{
    using System;

    public class QueryResult : IQueryResult
    {
        public QueryResult() { }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public QueryResult(string traceId)
            : this(traceId, ExecutionStatus.Success)
        {
        }

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public QueryResult(string traceId, ExecutionStatus status, string errorMessage = null)
        {
            this.TraceId = traceId;
            this.Status = status;
            this.ErrorMessage = errorMessage;
        }

        public string TraceId { get; set; }

        /// <summary>
        /// 失败的消息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 查询返回状态。
        /// </summary>
        public ExecutionStatus Status { get; set; }

        public DateTime ReplyTime { get; set; }


        public string ReplyServer { get; set; }


        public object Data { get; set; }

        public Type DataType { get; set; }
    }

    /// <summary>
    /// 查询结果
    /// </summary>
    public class QueryResult<TData> : IQueryResult<TData>
    {
        public QueryResult()
        { }

        public QueryResult(IQueryResult result)
        {
            this.ErrorMessage = result.ErrorMessage;
            this.Status = result.Status;

            if(result.Data is TData) {
                this.Data = (TData)result.Data;
            }
        }

        #region IQueryResult<TData> 成员

        public TData Data { get; set; }

        public ExecutionStatus Status { get; set; }

        public string ErrorMessage { get; set; }

        #endregion
    }
}
