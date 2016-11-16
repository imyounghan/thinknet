using System;
using System.Reflection;
using ThinkNet.Common;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling.Proxies
{
    public class MessageHandlerProxy : DisposableObject, IProxyHandler
    {
        private readonly IHandler handler;
        private readonly MethodInfo method;
        private readonly InterceptorPipeline pipeline;

        public MessageHandlerProxy(IHandler handler, MethodInfo method, InterceptorPipeline pipeline)
        {
            this.handler = handler;
            this.method = method;
            this.pipeline = pipeline;
        }

        public virtual void Handle(object[] args)
        {
            if (pipeline == null || pipeline.Count == 0) {
                method.Invoke(handler, args);
            }
            else {
                var input = new MethodInvocation(handler, method, args);
                pipeline.Invoke(input, delegate {
                    method.Invoke(handler, args);
                    return new MethodReturn(input, null, args);
                });
            }            
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            var lifecycle = LifeCycleAttribute.GetLifecycle(Method.DeclaringType);
            if(lifecycle == Lifecycle.Transient && disposing) {
                using(handler as IDisposable) {
                    // Dispose handler if it's disposable.
                }
            }
        }


        public MethodInfo Method { get { return this.method; } }

        public IHandler ReflectedHandler { get { return this.handler; } }
    }
}
