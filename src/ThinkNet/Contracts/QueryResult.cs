using System.Runtime.Serialization;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 查询结果
    /// </summary>
    [DataContract]
    public class QueryResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        [DataMember]
        public bool Success { get; set; }

        /// <summary>
        /// 成功或失败的消息
        /// </summary>
        [DataMember]
        public string Message { get; set; }
    }
}
