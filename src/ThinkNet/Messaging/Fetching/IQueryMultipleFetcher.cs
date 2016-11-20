using System.Collections.Generic;

namespace ThinkNet.Messaging.Fetching
{
    public interface IQueryMultipleFetcher<TParameter, TResult>
        where TParameter : QueryParameter
        //where TResult : QueryResult
    {
        /// <summary>
        /// 获取结果
        /// </summary>
        IEnumerable<TResult> Fetch(TParameter parameter);
    }
}
