using System;
using System.Reflection;
using ThinkNet.Common;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling.Agent
{
    public abstract class HandlerAgent : DisposableObject, IHandlerAgent
    {
        private readonly InterceptorPipeline pipeline;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected HandlerAgent(InterceptorPipeline pipeline)
        {
            this.pipeline = pipeline;
        }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public HandlerAgent(object handler, MethodInfo method, InterceptorPipeline pipeline)
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
            if(pipeline == null || pipeline.Count == 0) {
                TryMultipleHandle(args);
                return;
            }

            var input = new MethodInvocation(HandlerInstance, ReflectedMethod, args);
            var methodReturn = pipeline.Invoke(input, delegate {
                try {
                    TryMultipleHandle(args);
                    return new MethodReturn(input, null, args);
                }
                catch(Exception ex) {
                    return new MethodReturn(input, ex);
                }
            });

            if(methodReturn.Exception != null)
                throw methodReturn.Exception;
        }



        /// <summary>
        /// 尝试多次处理，默认只处理一次
        /// </summary>
        protected virtual void TryMultipleHandle(object[] args)
        {
            ReflectedMethod.Invoke(HandlerInstance, args);
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
