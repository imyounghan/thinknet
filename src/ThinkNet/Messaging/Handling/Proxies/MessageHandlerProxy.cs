using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using ThinkNet.Common;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Interception.Pipeline;
using ThinkNet.Runtime;

namespace ThinkNet.Messaging.Handling.Proxies
{
    /// <summary>
    /// 处理消息的代理程序
    /// </summary>
    public class MessageHandlerProxy : DisposableObject, IHandlerProxy
    {
        private static readonly int retryTimes = ConfigurationSetting.Current.HandleRetrytimes;
        private static readonly int retryInterval = ConfigurationSetting.Current.HandleRetryInterval;

        private readonly InterceptorPipeline pipeline;

        protected MessageHandlerProxy(InterceptorPipeline pipeline)
        {
            this.pipeline = pipeline;
        }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageHandlerProxy(object handler, MethodInfo method, InterceptorPipeline pipeline)
        {
            this.HandlerInstance = handler;
            this.ReflectedMethod = method;
            this.pipeline = pipeline;
        }

        /// <summary>
        /// 处理消息
        /// </summary>
        public virtual void Handle(object[] args)
        {
            Lazy<MethodInvocation> input = new Lazy<MethodInvocation>(() => new MethodInvocation(HandlerInstance, ReflectedMethod, args));

            int count = 0;
            while(count++ < retryTimes) {
                try {
                    TryMultipleHandle(input, args);
                }
                catch(ThinkNetException) {
                    throw;
                }
                catch(Exception ex) {
                    if(count == retryTimes) {
                        throw new ThinkNetException(ex.Message, ex);
                    }

                    if(LogManager.Default.IsWarnEnabled) {
                        LogManager.Default.Warn(ex,
                            "An exception happened while processing '{0}' through handler on '{1}', Error will be ignored and retry again({2}).",
                             args.Last(), ReflectedMethod.DeclaringType.FullName, count);
                    }
                    Thread.Sleep(retryInterval);
                }
            }
        }

        protected virtual void TryHandleWithoutPipeline(object[] args)
        {
            ReflectedMethod.Invoke(HandlerInstance, args);
        }


        private void TryMultipleHandle(Lazy<MethodInvocation> input, object[] args)
        {
            if(pipeline == null || pipeline.Count == 0) {
                ReflectedMethod.Invoke(HandlerInstance, args);
            }
            else {
                var methodReturn = pipeline.Invoke(input.Value, delegate {
                    ReflectedMethod.Invoke(HandlerInstance, args);
                    return new MethodReturn(input.Value, null, args);
                });

                if(methodReturn.Exception != null)
                    throw methodReturn.Exception;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            var lifecycle = LifeCycleAttribute.GetLifecycle(ReflectedMethod.DeclaringType);
            if(lifecycle == Lifecycle.Transient && disposing) {
                using(HandlerInstance as IDisposable) {
                    // Dispose handler if it's disposable.
                }
            }
        }

        /// <summary>
        /// 获取反射方法信息
        /// </summary>
        public virtual MethodInfo ReflectedMethod { get; private set; }
        /// <summary>
        /// 获取处理器的实例
        /// </summary>
        public object HandlerInstance { get; protected set; }
    }
}
