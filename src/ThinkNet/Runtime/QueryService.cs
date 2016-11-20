using System;
using System.Threading.Tasks;
using ThinkNet.Contracts;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Fetching;

namespace ThinkNet.Runtime
{
    public class QueryService : IQueryService
    {
        private readonly static TimeSpan WaitTime = TimeSpan.FromSeconds(5);
        private readonly static QueryResult TimeoutResult = new QueryResult(QueryStatus.Timeout, null);


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

        private IQueryResult FetchQueryResult(object parameter)
        {
            Type contractType;
            var fetcher = QueryFetcherProvider.Instance.GetFetcher(parameter.GetType(), out contractType);

            var genericType = contractType.GetGenericTypeDefinition();

            try {
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
    }
}
