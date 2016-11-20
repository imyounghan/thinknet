using System.Collections.Generic;

namespace ThinkNet.Messaging.Fetching
{
    public interface IQueryPageFetcher<TParameter, TResult>
        where TParameter : QueryPageParameter
        //where TResult : QueryResult
    {
        /// <summary>
        /// 获取结果
        /// </summary>
        IEnumerable<TResult> Fetch(TParameter parameter, out long total);
    }
}
