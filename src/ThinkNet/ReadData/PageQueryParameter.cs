using System.Runtime.Serialization;

namespace ThinkNet.ReadData
{
    /// <summary>
    /// 分页查询参数的抽象类
    /// </summary>
    [DataContract]
    public abstract class PageQueryParameter : QueryParameter
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
