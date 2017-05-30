using System.Collections.Generic;

namespace ThinkNet.Messaging.Fetching
{
    /// <summary>
    /// 用于分页查询的读取程序 
    /// </summary>
    public interface IQueryPageFetcher<TParameter, TResult>
        where TParameter : QueryParameter
        //where TResult : QueryResult
    {
        /// <summary>
        /// 获取结果
        /// </summary>
        IEnumerable<TResult> Fetch(TParameter parameter, out long total);
    }
}
