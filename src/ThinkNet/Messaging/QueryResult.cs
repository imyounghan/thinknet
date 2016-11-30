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
        public QueryResult()
            : this(QueryStatus.Success, null)
        { }

        public QueryResult(QueryStatus status, string errorMessage, string errorCode = null)
        {
            this.Status = status;
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
        }


        ///// <summary>
        ///// 是否成功
        ///// </summary>
        //[DataMember]
        //public bool Success { get; set; }

        /// <summary>
        /// 失败的消息编码
        /// </summary>
        [DataMember]
        public string ErrorCode { get; set; }

        /// <summary>
        /// 失败的消息
        /// </summary>
        [DataMember]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 查询返回状态。
        /// </summary>
        [DataMember]
        public QueryStatus Status { get; set; }
    }
}
