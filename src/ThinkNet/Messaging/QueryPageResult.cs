using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 分页数据
    /// </summary>
    [DataContract]
    public class QueryPageResult<T> : QueryMultipleResult<T>, IQueryPageResult<T>
    {
        /// <summary>
        /// 空数据
        /// </summary>
        public static readonly new QueryPageResult<T> Empty = new QueryPageResult<T>();

        public QueryPageResult()
        { }

        /// <summary>
        /// 构造实例
        /// </summary>
        /// <param name="totalRecords">总记录个数</param>
        /// <param name="pageSize">每页显示记录</param>
        /// <param name="pageIndex">当前页索引</param>
        /// <param name="datas">页面数据</param>
        public QueryPageResult(long totalRecords, int pageSize, int pageIndex, IEnumerable<T> datas)
            : base(datas)
        {
            this.TotalRecords = totalRecords;
            this.PageSize = pageSize;
            this.PageIndex = pageIndex;
            this.TotalPages = (long)Math.Ceiling((double)totalRecords / (double)pageSize);
        }

        /// <summary>
        /// 获取或设置总记录数。
        /// </summary>
        [DataMember]
        public long TotalRecords { get; set; }

        /// <summary>
        /// 获取或设置页数。
        /// </summary>
        [DataMember]
        public long TotalPages { get; set; }

        /// <summary>
        /// 获取或设置页面大小。
        /// </summary>
        [DataMember]
        public int PageSize { get; set; }

        /// <summary>
        /// 获取或设置页码。
        /// </summary>
        [DataMember]
        public int PageIndex { get; set; }
    }
}
