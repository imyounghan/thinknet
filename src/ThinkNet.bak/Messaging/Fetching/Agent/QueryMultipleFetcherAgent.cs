using System;
using ThinkNet.Contracts;
using ThinkNet.Infrastructure.Interception;

namespace ThinkNet.Messaging.Fetching.Agent
{
    /// <summary>
    /// 查询获取多个结果的代理
    /// </summary>
    public class QueryMultipleFetcherAgent<TParameter, TResult> : QueryFetcherAgent<TParameter>
        where TParameter : QueryParameter
    {
        private readonly IQueryMultipleFetcher<TParameter, TResult> fetcher;
        private readonly Type contractType;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public QueryMultipleFetcherAgent(IQueryMultipleFetcher<TParameter, TResult> fetcher,
            IInterceptorProvider interceptorProvider)
            : base(interceptorProvider)
        {
            this.fetcher = fetcher;
            this.contractType = typeof(IQueryMultipleFetcher<TParameter, TResult>);
        }

        /// <summary>
        /// 获取查询程序
        /// </summary>
        public override object GetInnerQueryFetcher()
        {
            return this.fetcher;
        }

        /// <summary>
        /// 获取查询接类型
        /// </summary>
        protected override Type GetQueryFetcherInterfaceType()
        {
            return this.contractType;
        }

        /// <summary>
        /// 尝试获取查询结果
        /// </summary>
        protected override IQueryResult TryFetch(TParameter parameter)
        {
            var result = fetcher.Fetch(parameter);
            return new QueryResultCollection<TResult>(result);
        }
    }
}
