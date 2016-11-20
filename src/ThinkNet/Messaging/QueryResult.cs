using System.Runtime.Serialization;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 查询结果
    /// </summary>
    [DataContract]
    public class QueryResult : IQueryResult
    {


        protected QueryResult()
        {
            this.Status = QueryStatus.Success;
        }

        public QueryResult(QueryStatus status, string message)
        {
            this.Status = status;
            this.Message = message;
        }

        ///// <summary>
        ///// 是否成功
        ///// </summary>
        //[DataMember]
        //public bool Success { get; set; }

        /// <summary>
        /// 成功或失败的消息
        /// </summary>
        [DataMember]
        public string Message { get; set; }

        /// <summary>
        /// 查询返回状态。
        /// </summary>
        [DataMember]
        public QueryStatus Status { get; set; }
    }
}
