using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示一个查询结果集合的接口
    /// </summary>
    public interface IQueryResultCollection<T> : IEnumerable<T>, IQueryResult
    {
        /// <summary>
        /// 获取指定索引处的元素。
        /// </summary>
        T this[int index] { get; }

        /// <summary>
        /// 当前集合的数量
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 获取或设置总记录数。
        /// </summary>
        long TotalRecords { get; }

        /// <summary>
        /// 获取或设置页数。
        /// </summary>
        long TotalPages { get; }

        /// <summary>
        /// 获取或设置页面大小。
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// 获取或设置页码。
        /// </summary>
        int PageIndex { get; }
    }
}
