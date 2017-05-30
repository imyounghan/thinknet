using System.Collections.Generic;

namespace ThinkNet.Messaging.Fetching
{
    /// <summary>
    /// 查询多个结果的读取程序
    /// </summary>
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
