using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using ThinkNet.Common;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Interception.Pipeline;
using ThinkNet.Runtime;

namespace ThinkNet.Messaging.Handling.Agent
{
    /// <summary>
    /// 处理消息的代理程序
    /// </summary>
    public class MessageHandlerAgent : DisposableObject, IHandlerAgent
    {
        private static readonly int retryTimes = ConfigurationSetting.Current.HandleRetrytimes;
        private static readonly int retryInterval = ConfigurationSetting.Current.HandleRetryInterval;

        private readonly InterceptorPipeline pipeline;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected MessageHandlerAgent(InterceptorPipeline pipeline)
        {
            this.pipeline = pipeline;
        }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageHandlerAgent(object handler, MethodInfo method, InterceptorPipeline pipeline)
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
                    break;
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
                            "An exception happened while handling '{0}' through handler on '{1}', Error will be ignored and retry again({2}).",
                             args.Last(), HandlerInstance.GetType().FullName, count);
                    }
                    Thread.Sleep(retryInterval);
                }
            }

            if (LogManager.Default.IsDebugEnabled) {
                LogManager.Default.DebugFormat("Handle '{0}' on '{1}' success.", args.Last(), HandlerInstance.GetType().FullName);
            }
        }

        /// <summary>
        /// 不经过拦截器管道的处理方式
        /// </summary>
        protected virtual void TryHandleWithoutPipeline(object[] args)
        {
            ReflectedMethod.Invoke(HandlerInstance, args);
        }


        private void TryMultipleHandle(Lazy<MethodInvocation> input, object[] args)
        {
            if(pipeline == null || pipeline.Count == 0) {
                TryHandleWithoutPipeline(args);
                return;
            }

            var methodReturn = pipeline.Invoke(input.Value, delegate {
                try {
                    TryHandleWithoutPipeline(args);
                    return new MethodReturn(input.Value, null, args);
                }
                catch (Exception ex) {
                    return new MethodReturn(input.Value, ex);
                }
            });

            if (methodReturn.Exception != null)
                throw methodReturn.Exception;
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
        public virtual object HandlerInstance { get; private set; }
    }
}
