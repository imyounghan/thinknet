using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using ThinkNet.Contracts;
using ThinkNet.Infrastructure.Interception;
using ThinkNet.Infrastructure.Interception.Pipeline;

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


    /// <summary>
    /// 查询代理
    /// </summary>
    public abstract class QueryFetcherAgent<TParameter> : IQueryFetcherAgent
        where TParameter : QueryParameter
    {
        private readonly static ConcurrentDictionary<Type, MethodInfo> FetchMethodCache = new ConcurrentDictionary<Type, MethodInfo>();

        private readonly IInterceptorProvider interceptorProvider;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        protected QueryFetcherAgent(IInterceptorProvider interceptorProvider)
        {
            this.interceptorProvider = interceptorProvider;
        }

        /// <summary>
        /// 尝试获取查询结果
        /// </summary>
        protected abstract IQueryResult TryFetch(TParameter parameter);

        /// <summary>
        /// 获取查询接口类型
        /// </summary>
        protected abstract Type GetQueryFetcherInterfaceType();

        /// <summary>
        /// 获取查询程序
        /// </summary>
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

        private IQueryResult Fetch(IQuery parameter)
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

        IQueryResult IQueryFetcherAgent.Fetch(IQuery parameter)
        {
            bool wasError = false;
            try {
                return this.Fetch(parameter as TParameter);
            }
            catch(Exception ex) {
                if(LogManager.Default.IsErrorEnabled) {
                    LogManager.Default.Error(ex, "Exception raised when fetching {0}.", parameter);
                }
                wasError = true;
                return new QueryResult(ReturnStatus.Failed, ex.Message);
            }
            finally {
                if(!wasError && LogManager.Default.IsDebugEnabled) {
                    LogManager.Default.DebugFormat("Fetch ({0}) on ({1}) successfully.",
                       parameter, this.GetInnerQueryFetcher().GetType().FullName);
                }
            }
        }
    }

    /// <summary>
    /// 查询返回单个值的代理程序
    /// </summary>
    public class QueryFetcherAgent<TParameter, TResult> : QueryFetcherAgent<TParameter>
        where TParameter : QueryParameter
    {
        private readonly IQueryFetcher<TParameter, TResult> fetcher;
        private readonly Type contractType;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public QueryFetcherAgent(IQueryFetcher<TParameter, TResult> fetcher, IInterceptorProvider interceptorProvider)
            : base(interceptorProvider)
        {
            this.fetcher = fetcher;
            this.contractType = typeof(IQueryFetcher<TParameter, TResult>);
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
        /// 尝试获取查询结果
        /// </summary>
        protected override IQueryResult TryFetch(TParameter parameter)
        {
            var result = fetcher.Fetch(parameter);
            return new QueryResultCollection<TResult>(result);
        }
    }
}
