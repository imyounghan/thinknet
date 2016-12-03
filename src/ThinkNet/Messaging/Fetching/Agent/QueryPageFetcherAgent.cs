using System;
using ThinkLib.Interception;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Fetching.Agent
{
    /// <summary>
    /// 分页查询代理程序
    /// </summary>
    public class QueryPageFetcherAgent<TParameter, TResult> : QueryFetcherAgent<TParameter>
        where TParameter : PagedQueryParameter
    {
        private readonly IQueryPageFetcher<TParameter, TResult> fetcher;
        private readonly Type contractType;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public QueryPageFetcherAgent(IQueryPageFetcher<TParameter, TResult> fetcher,
            IInterceptorProvider interceptorProvider)
            : base(interceptorProvider)
        {
            this.fetcher = fetcher;
            this.contractType = typeof(IQueryPageFetcher<TParameter, TResult>);
        }

        /// <summary>
        /// 获取查询程序
        /// </summary>
        public override object GetInnerQueryFetcher()
        {
            return this.fetcher;
        }

        /// <summary>
        /// 获取查询接口类型
        /// </summary>
        protected override Type GetQueryFetcherInterfaceType()
        {
            return this.contractType;
        }

        /// <summary>
        /// 尝试获取结果
        /// </summary>
        protected override IQueryResult TryFetch(TParameter parameter)
        {
            long total;
            var result = fetcher.Fetch(parameter, out total);
            return new QueryResultCollection<TResult>(total, parameter.PageSize, parameter.PageIndex, result);
        }

    }
}
