using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ThinkLib.Composition;
using ThinkNet.Contracts;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Fetching;

namespace ThinkNet.Runtime
{
    public class QueryService : IQueryService, IInitializer
    {
        private readonly static TimeSpan WaitTime = TimeSpan.FromSeconds(5);
        private readonly static QueryResult TimeoutResult = new QueryResult(QueryStatus.Timeout, null);
        private readonly static Dictionary<Type, Type> ParameterTypeMapFetcherType = new Dictionary<Type, Type>();

        private readonly IObjectContainer _container;

        public QueryService(IObjectContainer container)
        {
            this._container = container;
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
            return Task.Factory.StartNew(FetchQueryResult, queryParameter);
        }

        private object GetFetcher(Type parameterType, out Type contractType)
        {           ;
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


                if(genericType == typeof(IQueryFetcher<,>)) {
                    return QuerySingleResult(fetcher, parameter, contractType);
                }
                else if(genericType == typeof(IQueryMultipleFetcher<,>)) {
                    return QueryMultipleResult(fetcher, parameter, contractType);

                }
                else if(genericType == typeof(IQueryPageFetcher<,>)) {
                    return QueryPageResult(fetcher, parameter as QueryPageParameter, contractType);
                }
            }
            catch(Exception ex) {
                return new QueryResult(QueryStatus.Failed, ex.Message);
            }

            return new QueryResult(QueryStatus.Failed, "Unkown parameter");
        }

        private IQueryResult QueryPageResult(object fetcher, QueryPageParameter parameter, Type contractType)
        {

            dynamic total;
            var result = ((dynamic)fetcher).Fetch((dynamic)parameter, out total);
            var resultContractType = typeof(QueryPageResult<>).MakeGenericType(contractType.GenericTypeArguments[1]);
            var queryResult = Activator.CreateInstance(resultContractType, new object[] { total, parameter.PageSize, parameter.PageIndex, result });
            return queryResult as QueryResult;
        }

        private IQueryResult QuerySingleResult(object fetcher, object parameter, Type contractType)
        {
            var result = ((dynamic)fetcher).Fetch((dynamic)parameter);
            var resultContractType = typeof(QuerySingleResult<>).MakeGenericType(contractType.GenericTypeArguments[1]);
            var queryResult = Activator.CreateInstance(resultContractType, new object[] { result });
            return queryResult as QueryResult;
        }

        private IQueryResult QueryMultipleResult(object fetcher, object parameter, Type contractType)
        {
            var result = ((dynamic)fetcher).Fetch((dynamic)parameter);
            var resultContractType = typeof(QueryMultipleResult<>).MakeGenericType(contractType.GenericTypeArguments[1]);
            var queryResult = Activator.CreateInstance(resultContractType, new object[] { result });
            return queryResult as QueryResult;
        }

        public void Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            var queryFetcherInterfaceTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(FilterType);//.ToArray();

            foreach(var type in queryFetcherInterfaceTypes) {
                var parameterType = type.GetGenericArguments().First();
                if(ParameterTypeMapFetcherType.ContainsKey(null)) {
                    string errorMessage = string.Format("There are have duplicate IQueryFetcher interface type for {0}.", parameterType.FullName);
                    throw new ThinkNetException(errorMessage);
                }

                ParameterTypeMapFetcherType[parameterType] = type;
            }
        }

        private static bool FilterType(Type type)
        {
            if(!type.IsInterface || !type.IsGenericType)
                return false;

            var genericType = type.GetGenericTypeDefinition();

            return genericType == typeof(IQueryFetcher<,>) ||
                genericType == typeof(IQueryMultipleFetcher<,>) ||
                genericType == typeof(IQueryPageFetcher<,>);
        }
    }
}
