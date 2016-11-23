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

namespace ThinkNet.Messaging.Handling.Agent
{
    public abstract class HandlerAgent : DisposableObject, IHandlerAgent
    {
        private readonly static ConcurrentDictionary<Type, MethodInfo> HandleMethodCache = new ConcurrentDictionary<Type, MethodInfo>();

        //private readonly InterceptorPipeline pipeline;
        //private readonly MethodInfo reflectedMethod;
        private readonly object targetHandler;
        private readonly Type handlerInerfaceType;
        private readonly IInterceptorProvider _interceptorProvider;
        private readonly IEnumerable<IInterceptor> _firstInterceptors;
        private readonly IEnumerable<IInterceptor> _lastInterceptors;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected HandlerAgent(object handler)
        {
            this.targetHandler = handler;
        }

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected HandlerAgent(object handler, 
            IInterceptorProvider interceptorProvider,
            IEnumerable<IInterceptor> firstInterceptors,
            IEnumerable<IInterceptor> lastInterceptors)
        {
            this.targetHandler = handler;
        }
        ///// <summary>
        ///// Parameterized Constructor.
        ///// </summary>
        //protected HandlerAgent(InterceptorPipeline pipeline)
        //{
        //    //this.pipeline = pipeline;
        //}
        ///// <summary>
        ///// Parameterized Constructor.
        ///// </summary>
        //public HandlerAgent(object handler, MethodInfo method, InterceptorPipeline pipeline)
        //{
        //    //this.HandlerInstance = handler;
        //    //this.ReflectedMethod = method;
        //    //this.pipeline = pipeline;
        //}

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
            var input = new MethodInvocation(targetHandler, methodInfo, args);
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
            GetReflectedMethodInfo().Invoke(targetHandler, args);
        }

        protected T GetValue<T>(IEnumerable<object> args)
            where T : class
        {
            return args.FirstOrDefault(p => p.GetType() == typeof(T)) as T;
        }

        protected static void TryMultipleHandle(HandlerAgent handler, object[] args, int retryTimes = 3, int retryInterval = 1000)
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
                             args.Last(), handler.GetType().FullName, count);
                    }
                    Thread.Sleep(retryInterval);
                }
            }

            if (LogManager.Default.IsDebugEnabled) {
                LogManager.Default.DebugFormat("Handle '{0}' on '{1}' success.", args.Last(), handler.GetType().FullName);
            }
        }

        ///// <summary>
        ///// 尝试多次处理，默认只处理一次
        ///// </summary>
        //protected virtual void TryMultipleHandle(object[] args)
        //{
        //    //ReflectedMethod.Invoke(HandlerInstance, args);
        //}

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            var lifecycle = LifeCycleAttribute.GetLifecycle(targetHandler.GetType());
            if(lifecycle == Lifecycle.Transient && disposing) {
                using(targetHandler as IDisposable) {
                    // Dispose handler if it's disposable.
                }
            }
        }
        
        protected object GetTargetHandler()
        {
            return this.targetHandler;
        }

        protected virtual Type HandlerInterfaceType { get { return handlerInerfaceType; } }

        private MethodInfo GetReflectedMethodInfo()
        {
            if(handlerInerfaceType == null)
                return null;

            return HandleMethodCache.GetOrAdd(handlerInerfaceType, delegate (Type type) {
                var interfaceMap = GetTargetHandler().GetType().GetInterfaceMap(type);
                return interfaceMap.TargetMethods.FirstOrDefault();
            });
        }

        private IEnumerable<IInterceptor> GetInterceptors(MethodInfo method)
        {
            var defineInterceptors = Enumerable.Empty<IInterceptor>();
            var messageType = handlerInerfaceType.GetGenericArguments().FirstOrDefault();
            if(typeof(Command).IsAssignableFrom(messageType)) {
                defineInterceptors = _interceptorProvider.GetInterceptors(method);
            }            

            return _firstInterceptors.Concat(defineInterceptors).Concat(_lastInterceptors);
        }

        private InterceptorPipeline GetInterceptorPipeline()
        {
            var method = this.GetReflectedMethodInfo();
            if(method == null)
                return null;

            return InterceptorPipelineManager.Instance.CreatePipeline(method, GetInterceptors);
        }

    }
}
