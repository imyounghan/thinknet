using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using ThinkNet.Common;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Interception.Pipeline;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Runtime.Executing
{
    public abstract class Executor : IExecutor
    {
        private readonly static ConcurrentDictionary<string, MethodInfo> handleMethodCache = new ConcurrentDictionary<string, MethodInfo>();
        private readonly static InterceptorPipelineManager pipelineManager = new InterceptorPipelineManager();

        private readonly IInterceptorProvider _interceptorProvider;
        private readonly int _retryTimes;
        private readonly int _retryInterval;
        private int _executionCount;

        public Executor()
            : this(ConfigurationSetting.Current.HandleRetrytimes, ConfigurationSetting.Current.HandleRetryInterval)
        { }

        protected Executor(int retryTimes, int retryInterval)
        {
            this._retryInterval = retryInterval;
            this._retryTimes = retryTimes;
        }

        //protected IEnumerable<IHandler> GetHandlers(Type type)
        //{
        //    var contractType = typeof(IMessageHandler<>).MakeGenericType(type);
        //    return ObjectContainer.Instance.ResolveAll(contractType).Cast<IHandler>();
        //}

        protected abstract IEnumerable<IProxyHandler> GetProxyHandlers(Type type);

        protected TimeSpan TryMultipleInvokeHandler(object data, IProxyHandler handler, Stopwatch stopwatch)
        {
            _executionCount++;

            try {
                stopwatch.Restart();
                handler.Handle(data);
                stopwatch.Stop();

                return stopwatch.Elapsed;
            }
            catch (ThinkNetException) {
                throw;
            }
            catch (Exception ex) {
                if (_executionCount == _retryTimes) {
                    throw new ThinkNetException(ex.Message, ex);
                }

                if (LogManager.Default.IsWarnEnabled) {
                    LogManager.Default.Warn(ex,
                        "An exception happened while processing '{0}' through handler on '{1}', Error will be ignored and retry again({2}).",
                         data, handler.Method.DeclaringType.FullName, _executionCount);
                }
                Thread.Sleep(_retryInterval);

                return TryMultipleInvokeHandler(data, handler, stopwatch);
            }
        }



        //protected virtual void OnExecuting(TData data, Type handlerType)
        //{ }

        //protected virtual void OnExecuted(TData data, Type handlerType, Exception ex)
        //{ }

        //protected virtual void OnException(Exception ex)
        //{ }

        public void Execute(object data, out TimeSpan processTime)
        {
            data.NotNull("data");

            processTime = TimeSpan.Zero;
            

            var stopwatch = Stopwatch.StartNew();
            var handlers = GetProxyHandlers(data.GetType());
            foreach (var handler in handlers) {
                try {
                    processTime += TryMultipleInvokeHandler(data, handler, stopwatch);
                }
                catch (Exception ex) {
                    //TODO....WriteLog
                    //this.OnException(ex);
                }
            }
        }

        //private TimeSpan InvokeHandler(TData data, IProxyHandler handler, Stopwatch stopwatch)
        //{
        //    bool wasError = false;
        //    try {
        //        this.OnExecuting(data, handler.TargetType);
        //        return TryMultipleInvokeHandler(data, handler, stopwatch);
        //    }
        //    catch (Exception ex) {
        //        wasError = true;
        //        this.OnExecuted(data, handler.TargetType, ex);
        //        throw ex;
        //    }
        //    finally {
        //        if (!wasError) {
        //            this.OnExecuted(data, handler.TargetType, null);
        //        }
        //    }
        //}


        protected virtual MethodInfo GetHandleMethodInfo(Type type, Type contractType)
        {
            List<Type> parameTypes = new List<Type>(contractType.GenericTypeArguments);

            if(contractType == typeof(ICommandHandler<>)) {
                parameTypes.Insert(0, typeof(ICommandContext));
            }
            //else if(contractType == typeof(IEventHandler<>)) {
            //    parameTypes.Insert(0, typeof(VersionData));
            //}
            return type.GetMethod("Handle", parameTypes.ToArray());
        }

        protected MethodInfo GetCachedHandleMethodInfo(Type contractType, Func<Type> targetType)
        {
            var contractName = AttributedModelServices.GetContractName(contractType);

            return handleMethodCache.GetOrAdd(contractName, delegate (string key) {
                var method = this.GetHandleMethodInfo(targetType(), contractType);
                return method;
            });
        }

        protected MethodInfo GetCachedHandleMethodInfo(Type contractType, Func<Type> targetType, out InterceptorPipeline pipeline)
        {
            var contractName = AttributedModelServices.GetContractName(contractType);

            MethodInfo cachedMethod;
            if(!handleMethodCache.TryGetValue(contractName, out cachedMethod)) {
                cachedMethod = handleMethodCache.GetOrAdd(contractName, delegate (string key) {
                    var method = this.GetHandleMethodInfo(targetType(), contractType);
                    var interceptors = this.GetInterceptors(method);                    
                    if(!interceptors.IsEmpty()) {
                        pipelineManager.SetPipeline(method, new InterceptorPipeline(interceptors));
                    }
                    return method;
                });
            }

            pipeline = pipelineManager.GetPipeline(cachedMethod);
            return cachedMethod;
        }

        protected virtual IEnumerable<IInterceptor> GetInterceptors(MethodInfo method)
        {
            return _interceptorProvider.GetInterceptors(method);
        }

        protected class MessageHandledInterceptor : IInterceptor
        {
            private readonly IMessageHandlerRecordStore _handlerStore;

            public MessageHandledInterceptor(IMessageHandlerRecordStore handlerStore)
            {
                this._handlerStore = handlerStore;
            }


            #region IInterceptor 成员

            public IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptorDelegate getNext)
            {
                var parameterName = input.Arguments.GetParameterInfo(input.Arguments.Count - 1).Name;
                var message = input.Arguments[parameterName] as IUniquelyIdentifiable;
                var messageType = message.GetType();
                var handlerType = input.Target.GetType();

                if(_handlerStore.HandlerIsExecuted(message.UniqueId, messageType, handlerType)) {
                    var errorMessage = string.Format("The message has been handled. MessageHandlerType:{0}, MessageType:{1}, MessageId:{2}.",
                        handlerType.FullName, messageType.FullName, message.UniqueId);
                    //throw new ThinkNetException(errorMessage);
                    return new MethodReturn(input, new ThinkNetException(errorMessage));
                }

                var methodReturn = getNext().Invoke(input, getNext);

                if(methodReturn.Exception != null)
                    _handlerStore.AddHandlerInfo(message.UniqueId, messageType, handlerType);

                return methodReturn;
            }

            #endregion
        }
    }
}
