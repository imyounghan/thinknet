namespace ThinkNet.Messaging.Fetching
{
    /// <summary>
    /// 继承该接口的是查询执行器
    /// </summary>
    public interface IQueryFetcher<TParameter, TResult>
        where TParameter : QueryParameter
        //where TResult : QueryResult
    {
        /// <summary>
        /// 获取结果
        /// </summary>
        TResult Fetch(TParameter parameter);
    }    
}
