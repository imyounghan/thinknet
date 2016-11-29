using System;
using ThinkLib.Interception;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Fetching.Agent
{
    public class QueryMultipleFetcherAgent<TParameter, TResult> : QueryFetcherAgent<TParameter>
        where TParameter : QueryParameter
    {
        private readonly IQueryMultipleFetcher<TParameter, TResult> fetcher;
        private readonly Type contractType;

        public QueryMultipleFetcherAgent(IQueryMultipleFetcher<TParameter, TResult> fetcher,
            IInterceptorProvider interceptorProvider)
            : base(interceptorProvider)
        {
            this.fetcher = fetcher;
            this.contractType = typeof(IQueryMultipleFetcher<TParameter, TResult>);
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
            var result = fetcher.Fetch(parameter);
            return new QueryMultipleResult<TResult>(result);
        }
    }
}
