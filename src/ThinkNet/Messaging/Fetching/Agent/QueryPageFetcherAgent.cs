using System;
using ThinkLib.Interception;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Fetching.Agent
{
    public class QueryPageFetcherAgent<TParameter, TResult> : QueryFetcherAgent<TParameter>
        where TParameter : QueryPageParameter
    {
        private readonly IQueryPageFetcher<TParameter, TResult> fetcher;
        private readonly Type contractType;

        public QueryPageFetcherAgent(IQueryPageFetcher<TParameter, TResult> fetcher,
            IInterceptorProvider interceptorProvider)
            : base(interceptorProvider)
        {
            this.fetcher = fetcher;
            this.contractType = typeof(IQueryPageFetcher<TParameter, TResult>);
        }

        public override object GetInnerQueryFetcher()
        {
            return this.fetcher;
        }

        protected override Type GetQueryFetcherInterfaceType()
        {
            return this.contractType;
        }

        protected override IQueryResult TryFetch(TParameter parameter)
        {
            long total;
            var result = fetcher.Fetch(parameter, out total);
            return new QueryPageResult<TResult>(total, parameter.PageSize, parameter.PageIndex, result);
        }

    }
}
