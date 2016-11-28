using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ThinkLib;
using ThinkLib.Annotation;
using ThinkLib.Composition;
using ThinkLib.Interception;
using ThinkLib.Interception.Pipeline;
using ThinkNet.Runtime;

namespace ThinkNet.Messaging.Handling.Agent
{
    public abstract class HandlerAgent : DisposableObject, IHandlerAgent
    {
        private readonly static ConcurrentDictionary<Type, MethodInfo> HandleMethodCache = new ConcurrentDictionary<Type, MethodInfo>();

        private readonly bool _enableFilter;
        private readonly IInterceptorProvider _interceptorProvider;
        private readonly IEnumerable<IInterceptor> _firstInterceptors;
        private readonly IEnumerable<IInterceptor> _lastInterceptors;


        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected HandlerAgent(IInterceptorProvider interceptorProvider)
            : this(interceptorProvider, null, null)
        { }

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected HandlerAgent(IInterceptorProvider interceptorProvider,
            IEnumerable<IInterceptor> firstInterceptors,
            IEnumerable<IInterceptor> lastInterceptors)
        {
            this._interceptorProvider = interceptorProvider;
            this._firstInterceptors = firstInterceptors ?? Enumerable.Empty<IInterceptor>();
            this._lastInterceptors = lastInterceptors ?? Enumerable.Empty<IInterceptor>();
            this._enableFilter = !interceptorProvider.IsNull();
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        public virtual void Handle(object[] args)
        {
            var pipeline = this.GetInterceptorPipeline();
            if(pipeline == null || pipeline.Count == 0) {
                TryMultipleHandle(this, args);
                return;
            }

            var methodInfo = this.GetReflectedMethodInfo();
            var input = new MethodInvocation(GetInnerHandler(), methodInfo, args);
            var methodReturn = pipeline.Invoke(input, delegate {
                try {
                    TryMultipleHandle(this, args);
                    return new MethodReturn(input, null, args);
                }
                catch(Exception ex) {
                    return new MethodReturn(input, ex);
                }
            });

            if(methodReturn.Exception != null)
                throw methodReturn.Exception;
        }

        protected virtual void TryHandle(object[] args)
        {
            GetReflectedMethodInfo().Invoke(GetInnerHandler(), args);
        }

        //protected T GetValue<T>(IEnumerable<object> args)
        //    where T : class
        //{
        //    return args.FirstOrDefault(p => p.GetType() == typeof(T)) as T;
        //}

        private static void TryMultipleHandle(HandlerAgent handler, object[] args)
        {
            //private static readonly int retryTimes = ConfigurationSetting.Current.HandleRetrytimes;
            //private static readonly int retryInterval = ConfigurationSetting.Current.HandleRetryInterval;

            TryMultipleHandle(handler, args,
                ConfigurationSetting.Current.HandleRetrytimes,
                ConfigurationSetting.Current.HandleRetryInterval);
        }

        private static void TryMultipleHandle(HandlerAgent handler, object[] args, int retryTimes, int retryInterval)
        {
            int count = 0;
            while (count++ < retryTimes) {
                try {
                    handler.TryHandle(args);
                    break;
                }
                catch (ThinkNetException) {
                    throw;
                }
                catch (Exception ex) {
                    if (count == retryTimes) {
                        throw new ThinkNetException(ex.Message, ex);
                    }
                    if (LogManager.Default.IsWarnEnabled) {
                        LogManager.Default.Warn(ex,
                            "An exception happened while handling '{0}' through handler on '{1}', Error will be ignored and retry again({2}).",
                             args.Last(), handler.GetInnerHandler().GetType().FullName, count);
                    }
                    Thread.Sleep(retryInterval);
                }
            }

            if (LogManager.Default.IsDebugEnabled) {
                LogManager.Default.DebugFormat("Handle '{0}' on '{1}' success.", args.Last(), handler.GetInnerHandler().GetType().FullName);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            var lifecycle = LifeCycleAttribute.GetLifecycle(GetInnerHandler().GetType());
            if(lifecycle == Lifecycle.Transient && disposing) {
                using (GetInnerHandler() as IDisposable) {
                    // Dispose handler if it's disposable.
                }
            }
        }

        protected abstract Type GetHandlerInterfaceType();

        public abstract object GetInnerHandler();
        
        private MethodInfo GetReflectedMethodInfo()
        {
            return HandleMethodCache.GetOrAdd(GetHandlerInterfaceType(), delegate(Type type) {
                var interfaceMap = GetInnerHandler().GetType().GetInterfaceMap(type);
                return interfaceMap.TargetMethods.FirstOrDefault();
            });
        }

        private IEnumerable<IInterceptor> GetInterceptors(MethodInfo method)
        {
            var defineInterceptors = _interceptorProvider.GetInterceptors(method);

            return _firstInterceptors.Concat(defineInterceptors).Concat(_lastInterceptors);
        }

        protected InterceptorPipeline GetInterceptorPipeline()
        {
            if (!_enableFilter) {
                var combinedInterceptors = _firstInterceptors.Concat(_lastInterceptors);
                if (combinedInterceptors.IsEmpty())
                    return null;

                return new InterceptorPipeline(combinedInterceptors);
            }

            var method = this.GetReflectedMethodInfo();
            if(method == null)
                return new InterceptorPipeline(_firstInterceptors.Concat(_lastInterceptors));

            return InterceptorPipelineManager.Instance.CreatePipeline(method, GetInterceptors);
        }

    }
}
