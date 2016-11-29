using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ThinkLib.Composition;
using ThinkLib.Interception;
using ThinkNet.Contracts;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Fetching;
using ThinkNet.Messaging.Fetching.Agent;

namespace ThinkNet.Runtime
{
    public class QueryService : MarshalByRefObject, IQueryService, IInitializer
    {
        private readonly static TimeSpan WaitTime = TimeSpan.FromSeconds(5);
        private readonly static QueryResult TimeoutResult = new QueryResult(QueryStatus.Timeout, null);
        private readonly static Dictionary<Type, Type> ParameterTypeMapFetcherType = new Dictionary<Type, Type>();

        private readonly IObjectContainer _container;
        private readonly IInterceptorProvider _interceptorProvider;

        public QueryService(IObjectContainer container, IInterceptorProvider interceptorProvider)
        {
            this._container = container;
            this._interceptorProvider = interceptorProvider;
        }

        public IQueryResult Execute(IQueryParameter queryParameter)
        {
            var task = this.ExecuteAsync(queryParameter);
            if(task.Wait(WaitTime)) {
                return task.Result;
            }

            return TimeoutResult;
        }

        public Task<IQueryResult> ExecuteAsync(IQueryParameter queryParameter)
        {
            return Task.Factory.StartNew<IQueryResult>(FetchQueryResult, queryParameter);
        }

        private object GetFetcher(Type parameterType, out Type contractType)
        {
            if(!ParameterTypeMapFetcherType.TryGetValue(parameterType, out contractType)) {
                throw new QueryFetcherNotFoundException(parameterType);
            }

            //var contractType = typeof(IQueryFetcher<>).MakeGenericType(queryParameterType);
            var fetchers = _container.ResolveAll(contractType).ToArray();

            switch(fetchers.Length) {
                case 0:
                    throw new QueryFetcherNotFoundException(parameterType);
                case 1:
                    return fetchers[0];
                default:
                    throw new QueryFetcherTooManyException(parameterType);
            }
        }

        private IQueryResult FetchQueryResult(object parameter)
        {        
            try {
                Type contractType;
                var fetcher = this.GetFetcher(parameter.GetType(), out contractType);
                var genericType = contractType.GetGenericTypeDefinition();
                var agentType = (Type)null;

                if(genericType == typeof(IQueryFetcher<,>)) {
                    agentType = typeof(QueryFetcherAgent<,>).MakeGenericType(contractType.GetGenericArguments());
                }
                else if(genericType == typeof(IQueryMultipleFetcher<,>)) {
                    agentType = typeof(QueryMultipleFetcherAgent<,>).MakeGenericType(contractType.GetGenericArguments());

                }
                else if(genericType == typeof(IQueryPageFetcher<,>)) {
                    agentType = typeof(QueryPageFetcherAgent<,>).MakeGenericType(contractType.GetGenericArguments());
                }
                else {
                    return new QueryResult(QueryStatus.Failed, "Unkown parameter");
                }

                var agent = (IQueryFetcherAgent)Activator.CreateInstance(agentType, new object[] { fetcher, _interceptorProvider });
                return agent.Fetch((IQueryParameter)parameter);
            }
            catch(Exception ex) {
                return new QueryResult(QueryStatus.Failed, ex.Message);
            }            
        }
        
        public void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            var filteredTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(FilterType)
                .SelectMany(type => type.GetInterfaces())
                .Where(FilterInterfaceType);

            foreach(var type in filteredTypes) {
                var parameterType = type.GetGenericArguments().First();
                if(ParameterTypeMapFetcherType.ContainsKey(parameterType)) {
                    string errorMessage = string.Format("There are have duplicate IQueryFetcher interface type for {0}.", parameterType.FullName);
                    throw new ThinkNetException(errorMessage);
                }

                ParameterTypeMapFetcherType[parameterType] = type;
            }
        }

        private static bool FilterInterfaceType(Type type)
        {
            if(!type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IQueryFetcher<,>) ||
                genericType == typeof(IQueryMultipleFetcher<,>) ||
                genericType == typeof(IQueryPageFetcher<,>);
        }

        private static bool FilterType(Type type)
        {
            if(!type.IsClass || type.IsAbstract)
                return false;

            return type.GetInterfaces().Any(FilterInterfaceType);
        }
    }
}
