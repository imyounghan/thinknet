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
        /// <summary>
        /// Default constructor.
        /// </summary>
        public QueryResult()
            : this(ReturnStatus.Success, null)
        { }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public QueryResult(ReturnStatus status, string errorMessage)
        {
            this.Status = status;
            this.ErrorMessage = errorMessage;
        }
        

        /// <summary>
        /// 失败的消息
        /// </summary>
        [DataMember]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 查询返回状态。
        /// </summary>
        [DataMember]
        public ReturnStatus Status { get; set; }
    }
}
