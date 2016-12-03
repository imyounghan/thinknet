using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ThinkLib;
using ThinkLib.Annotation;
using ThinkLib.Composition;
using ThinkLib.Interception;
using ThinkNet.Contracts;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Fetching;
using ThinkNet.Messaging.Fetching.Agent;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 用于查询的调度程序
    /// </summary>
    public class QueryDispatcher : IDispatcher, IInitializer
    {
        private readonly Dictionary<Type, Type> _parameterTypeMapFetcherType;
        private readonly ConcurrentDictionary<string, IQueryFetcherAgent> _cachedFetchers;
        private readonly IObjectContainer _container;
        private readonly IInterceptorProvider _interceptorProvider;
        private readonly IQueryResultNotification _notification;

        /// <summary>
        /// Parameterized constructor.
        /// </summary>
        public QueryDispatcher(IObjectContainer container, IInterceptorProvider interceptorProvider, IQueryResultNotification notification)
        {
            this._container = container;
            this._interceptorProvider = interceptorProvider;
            this._notification = notification;
            this._parameterTypeMapFetcherType = new Dictionary<Type, Type>();
            this._cachedFetchers = new ConcurrentDictionary<string, IQueryFetcherAgent>();
        }

        private void Execute(QueryParameter parameter)
        {
            try {
                var fetcher = this.GetProxyFetcher(parameter.GetType());
                var result = fetcher.Fetch(parameter);
                _notification.Notify(parameter.Id, result);
            }
            catch(Exception ex) {
                _notification.Notify(parameter.Id, new QueryResult(ReturnStatus.Failed, ex.Message));
                throw ex;
            }
        }

        private object GetFetcher(Type parameterType, out Type contractType)
        {
            if(!_parameterTypeMapFetcherType.TryGetValue(parameterType, out contractType)) {
                throw new QueryFetcherNotFoundException(parameterType);
            }

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

        private IQueryFetcherAgent GetProxyFetcher(Type parameterType)
        {
            IQueryFetcherAgent cachedFetcher;
            if(_cachedFetchers.TryGetValue(parameterType.FullName, out cachedFetcher))
                return cachedFetcher;

            Type contractType;
            var fetcher = this.GetFetcher(parameterType, out contractType);
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
                throw new ThinkNetException(string.Format("Unkown parameter of '{0}'.", parameterType.FullName));
            }

            cachedFetcher = (IQueryFetcherAgent)Activator.CreateInstance(agentType, 
                new object[] { fetcher, _interceptorProvider });

            var lifecycle = LifeCycleAttribute.GetLifecycle(fetcher.GetType());
            if(lifecycle == Lifecycle.Singleton)
                _cachedFetchers.TryAdd(parameterType.FullName, cachedFetcher);

            return cachedFetcher;
        }



        #region IDispatcher 成员

        void IDispatcher.Execute(IMessage message, out TimeSpan executionTime)
        {
            message.NotNull("message");

            executionTime = TimeSpan.Zero;


            var stopwatch = Stopwatch.StartNew();
            try {
                stopwatch.Restart();
                this.Execute(message as QueryParameter);
                stopwatch.Stop();

                executionTime = stopwatch.Elapsed;                
            }
            catch(Exception ex) {
                if(LogManager.Default.IsErrorEnabled) {
                    LogManager.Default.Error(ex, "Exception raised when fetching ({0}).", message);
                }
            }
        }

        #endregion

        #region IInitializer 成员

        void IInitializer.Initialize(IObjectContainer container, IEnumerable<Assembly> assemblies)
        {
            var filteredTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(FilterType)
                .SelectMany(type => type.GetInterfaces())
                .Where(FilterInterfaceType);

            foreach(var type in filteredTypes) {
                var parameterType = type.GetGenericArguments().First();
                if(_parameterTypeMapFetcherType.ContainsKey(parameterType)) {
                    string errorMessage = string.Format("There are have duplicate IQueryFetcher interface type for {0}.", parameterType.FullName);
                    throw new ThinkNetException(errorMessage);
                }

                _parameterTypeMapFetcherType[parameterType] = type;
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
        #endregion
    }
}
