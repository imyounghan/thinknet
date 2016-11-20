using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common.Composition;

namespace ThinkNet.Messaging.Fetching
{
    public class QueryFetcherProvider
    {
        public static readonly QueryFetcherProvider Instance = new QueryFetcherProvider();

        private readonly Dictionary<Type, Type> ParameterTypeMapFetcherType;

        private QueryFetcherProvider()
        {
            this.ParameterTypeMapFetcherType = new Dictionary<Type, Type>();
        }

        public Type GetFetcherType(Type parameterType)
        {
            Type contractType;
            if(!ParameterTypeMapFetcherType.TryGetValue(parameterType, out contractType)) {
                throw new QueryFetcherNotFoundException(parameterType);
            }

            return contractType;
        }

        public object GetFetcher(Type parameterType)
        {
            Type contractType = GetFetcherType(parameterType);

            //var contractType = typeof(IQueryFetcher<>).MakeGenericType(queryParameterType);
            var fetchers = ObjectContainer.Instance.ResolveAll(contractType).ToArray();

            switch(fetchers.Length) {
                case 0:
                    throw new QueryFetcherNotFoundException(parameterType);
                case 1:
                    return fetchers[0];
                default:
                    throw new QueryFetcherTooManyException(parameterType);
            }
        }

        public object GetFetcher(Type parameterType, out Type contractType)
        {
            contractType = GetFetcherType(parameterType);
            
            var fetchers = ObjectContainer.Instance.ResolveAll(contractType).ToArray();

            switch(fetchers.Length) {
                case 0:
                    throw new QueryFetcherNotFoundException(parameterType);
                case 1:
                    return fetchers[0];
                default:
                    throw new QueryFetcherTooManyException(parameterType);
            }
        }
    }
}
