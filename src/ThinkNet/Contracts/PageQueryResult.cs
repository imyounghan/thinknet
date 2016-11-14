using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace ThinkNet.Contracts
{
    [DataContract]
    public class PageQueryResult<T> : QueryResult
    {
        /// <summary>
        /// 空数据
        /// </summary>
        public static readonly PageQueryResult<T> Empty = new PageQueryResult<T>();

        private PageQueryResult()
        {
            this.Data = Enumerable.Empty<T>();
        }

        /// <summary>
        /// 构造实例
        /// </summary>
        /// <param name="totalRecords">总记录个数</param>
        /// <param name="pageSize">每页显示记录</param>
        /// <param name="pageIndex">当前页索引</param>
        /// <param name="data">页面数据</param>
        public PageQueryResult(int totalRecords, int pageSize, int pageIndex, IEnumerable<T> data)
        {
            this.TotalRecords = totalRecords;
            this.PageSize = pageSize;
            this.PageIndex = pageIndex;
            this.TotalPages = (int)Math.Ceiling((double)totalRecords / (double)pageSize);

            this.Data = data ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// 获取或设置总记录数。
        /// </summary>
        [DataMember]
        public int TotalRecords { get; private set; }

        /// <summary>
        /// 获取或设置页数。
        /// </summary>
        [DataMember]
        public int TotalPages { get; private set; }

        /// <summary>
        /// 获取或设置页面大小。
        /// </summary>
        [DataMember]
        public int PageSize { get; private set; }

        /// <summary>
        /// 获取或设置页码。
        /// </summary>
        [DataMember]
        public int PageIndex { get; private set; }

        /// <summary>
        /// 获取或设置页面数据
        /// </summary>
        [DataMember]
        public IEnumerable<T> Data { get; private set; }
    }
}
