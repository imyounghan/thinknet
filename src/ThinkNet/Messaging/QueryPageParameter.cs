using System.Runtime.Serialization;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 分页查询参数的抽象类
    /// </summary>
    [DataContract]
    public abstract class QueryPageParameter : QueryParameter, IQueryPageParameter
    {
        /// <summary>
        /// 当前页码
        /// </summary>
        [DataMember]
        public int PageIndex { get; set; }
        /// <summary>
        /// 当前页显示数量大小
        /// </summary>
        [DataMember]
        public int PageSize { get; set; }
    }
}
