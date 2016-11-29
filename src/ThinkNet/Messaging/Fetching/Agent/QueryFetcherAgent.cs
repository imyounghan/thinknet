using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ThinkLib.Interception;
using ThinkLib.Interception.Pipeline;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging.Fetching.Agent
{
    //public class QueryFetcherAgent : IQueryFetcherAgent
    //{
    //    private readonly object fetcher;
    //    private readonly Type contractType;
    //    protected virtual IQueryResult TryFetch(IQueryParameter parameter)
    //    {
    //        var result = ((dynamic)fetcher).Fetch((dynamic)parameter);
    //        var genericTypes = contractType.GetGenericArguments();
    //        var resultContractType = typeof(QuerySingleResult<>).MakeGenericType(genericTypes[1]);
    //        var queryResult = Activator.CreateInstance(resultContractType, new object[] { result });
    //        return queryResult as QueryResult;
    //    }


    //    IQueryResult IQueryFetcherAgent.Fetch(IQueryParameter parameter)
    //    {
    //        try {
    //            return TryFetch(parameter);
    //        }
    //        catch(Exception ex) {
    //            return new QueryResult(QueryStatus.Failed, ex.Message);
    //        }
    //    }
    //}

    public abstract class QueryFetcherAgent<TParameter> : IQueryFetcherAgent
        where TParameter : QueryParameter
    {
        private readonly static ConcurrentDictionary<Type, MethodInfo> FetchMethodCache = new ConcurrentDictionary<Type, MethodInfo>();

        private readonly IInterceptorProvider interceptorProvider;

        protected QueryFetcherAgent(IInterceptorProvider interceptorProvider)
        {
            this.interceptorProvider = interceptorProvider;
        }

        protected abstract IQueryResult TryFetch(TParameter parameter);

        protected abstract Type GetQueryFetcherInterfaceType();

        public abstract object GetInnerQueryFetcher();

        private MethodInfo GetReflectedMethodInfo()
        {
            return FetchMethodCache.GetOrAdd(GetQueryFetcherInterfaceType(), delegate(Type type) {
                var interfaceMap = GetInnerQueryFetcher().GetType().GetInterfaceMap(type);
                return interfaceMap.TargetMethods.FirstOrDefault();
            });
        }

        private InterceptorPipeline GetInterceptorPipeline()
        {
            var method = this.GetReflectedMethodInfo();
            if(method == null)
                return InterceptorPipeline.Empty;

            return InterceptorPipelineManager.Instance.CreatePipeline(method, interceptorProvider.GetInterceptors);
        }

        private IQueryResult Fetch(IQueryParameter parameter)
        {
            var pipeline = this.GetInterceptorPipeline();
            if(pipeline == null || pipeline.Count == 0) {
                return TryFetch(parameter as TParameter);
            }

            var methodInfo = this.GetReflectedMethodInfo();
            var input = new MethodInvocation(GetInnerQueryFetcher(), methodInfo, parameter);
            var methodReturn = pipeline.Invoke(input, delegate {
                try {
                    var result = TryFetch(parameter as TParameter);
                    return new MethodReturn(input, result, new object[] { parameter });
                }
                catch(Exception ex) {
                    return new MethodReturn(input, ex);
                }
            });

            if(methodReturn.Exception != null)
                throw methodReturn.Exception;

            return methodReturn.ReturnValue as IQueryResult;
        }

        IQueryResult IQueryFetcherAgent.Fetch(IQueryParameter parameter)
        {
            try {
                return this.Fetch(parameter as TParameter);
            }
            catch(Exception ex) {
                return new QueryResult(QueryStatus.Failed, ex.Message);
            }
        }
    }

    public class QueryFetcherAgent<TParameter, TResult> : QueryFetcherAgent<TParameter>
        where TParameter : QueryParameter
    {
        private readonly IQueryFetcher<TParameter, TResult> fetcher;
        private readonly Type contractType;

        public QueryFetcherAgent(IQueryFetcher<TParameter, TResult> fetcher, IInterceptorProvider interceptorProvider)
            : base(interceptorProvider)
        {
            this.fetcher = fetcher;
            this.contractType = typeof(IQueryFetcher<TParameter, TResult>);
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
            return new QuerySingleResult<TResult>(result);
        }
    }
}
