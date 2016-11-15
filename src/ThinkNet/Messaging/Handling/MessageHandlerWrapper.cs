using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using ThinkNet.Common;
using ThinkNet.Common.Composition;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Interception.Pipeline;

namespace ThinkNet.Messaging.Handling
{
    public class MessageHandlerWrapper : DisposableObject, IProxyHandler
    {
        private readonly ConcurrentDictionary<string, MethodInfo> handleMethodCache = new ConcurrentDictionary<string, MethodInfo>();
        private readonly InterceptorPipelineManager pipelineManager = new InterceptorPipelineManager();

        private readonly IHandler handler;

        public MessageHandlerWrapper(IHandler handler, Type contractType)
        {
            this.ContractType = contractType;
            this.handler = handler;
            this.TargetType = handler.GetType();
        }

        public virtual void Handle(object handler, object[] args)
        {
            InterceptorPipeline pipeline;
            var method = GetHandleMethodInfo(out pipeline);

            if (pipeline.Count == 0 || !(args.First() is ICommand)) {
                method.Invoke(handler, args);
            }
            else {
                var input = new MethodInvocation(handler, method, args);
                pipeline.Invoke(input, delegate {
                    method.Invoke(handler, args);
                    return new MethodReturn(input, null, args);
                });
            }

            //var message = args.First();
            //((dynamic)handler).Handle((dynamic)message);
        }

        protected virtual MethodInfo GetHandleMethodInfo(Type targetType, Type[] parameterTypes)
        {
            return targetType.GetMethod("Handle", parameterTypes);
        }

        protected MethodInfo GetHandleMethodInfo()
        {
            var contractName = AttributedModelServices.GetContractName(this.ContractType);

            var methodInfo = handleMethodCache.GetOrAdd(contractName, delegate (string key) {
                var method = GetHandleMethodInfo(this.TargetType, this.ContractType.GenericTypeArguments);

                return method;
            });

            return methodInfo;
        }

        protected MethodInfo GetHandleMethodInfo(out InterceptorPipeline pipeline)
        {
            var contractName = AttributedModelServices.GetContractName(this.ContractType);

            var methodInfo = handleMethodCache.GetOrAdd(contractName, delegate(string key) {
                var method = GetHandleMethodInfo(this.TargetType, this.ContractType.GenericTypeArguments);

                var interceptor = method.GetAttributes<InterceptorAttribute>(false)
                    .Concat(ContractType.GetAttributes<InterceptorAttribute>(false))
                    .OrderBy(p => p.Order)
                    .Select(p => p.CreateInterceptor(ObjectContainer.Instance))
                    .ToArray();
                if (interceptor.Length > 0) {
                    pipelineManager.SetPipeline(method, new InterceptorPipeline(interceptor));
                }

                return method;
            });

            pipeline = pipelineManager.GetPipeline(methodInfo);

            return methodInfo;
        }

        /// <summary>
        /// dispose
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (LifeCycleAttribute.GetLifecycle(this.TargetType) == Lifecycle.Transient && disposing) {
                using (handler as IDisposable) {
                    // Dispose handler if it's disposable.
                }
            }
        }


        public Type ContractType { get; private set; }

        public Type TargetType { get; private set; }

        public IHandler GetTargetHandler()
        {
            return this.handler;
        }

        #region IProxyHandler 成员

        void IProxyHandler.Handle(params object[] args)
        {
            //var method = GetHandleMethod();
            //var pipeline = pipelineManager.GetPipeline(method);
            //if (pipeline.Count == 0) {
            //    this.Handle(handler, args);
            //}
            //else {
            //    pipeline.Invoke(null, delegate {

            //        return new MethodReturn(null, null, args);
            //    });
            //}
            this.Handle(handler, args);
        }
        #endregion
    }
}
