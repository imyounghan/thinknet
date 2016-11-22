using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkLib;
using ThinkLib.Annotation;
using ThinkLib.Composition;
using ThinkLib.Interception;
using ThinkLib.Interception.Pipeline;
using ThinkNet.Domain;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Agent;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 处理命令的代理程序
    /// </summary>
    public class CommandDispatcher : MessageDispatcher
    {
        private readonly Func<CommandContext> _commandContextFactory;
        private readonly IInterceptor _firstInterceptor;
        private readonly IInterceptor _lastInterceptor;
        private readonly IInterceptorProvider _interceptorProvider;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CommandDispatcher(IRepository repository, 
            IEventSourcedRepository eventSourcedRepository,
            IMessageBus messageBus, 
            IMessageHandlerRecordStore handlerStore,
            IInterceptorProvider interceptorProvider)
        {
            this._commandContextFactory = () => new CommandContext(repository, eventSourcedRepository, messageBus);
            this._firstInterceptor = new FilterHandledMessageInterceptor(handlerStore);
            this._lastInterceptor = new NotifyCommandResultInterceptor(messageBus);
            this._interceptorProvider = interceptorProvider;
        }

        private InterceptorPipeline GetInterceptorPipeline(MethodInfo method)
        {
            Func<IEnumerable<IInterceptor>> getInterceptors = delegate {
                List<IInterceptor> interceptors = new List<IInterceptor>();
                interceptors.Add(_firstInterceptor);
                interceptors.AddRange(_interceptorProvider.GetInterceptors(method));
                interceptors.Add(_lastInterceptor);

                return interceptors;
            };

            return InterceptorPipelineManager.Instance.CreatePipeline(method, getInterceptors);
        }

        private IHandlerAgent BuildCommandHandler(object handler, Type contractType)
        {
            var method = MessageHandlerProvider.Instance.GetCachedHandleMethodInfo(contractType, () => handler.GetType());
            var pipeline = GetInterceptorPipeline(method);
            return new CommandHandlerAgent(handler, method, pipeline, _commandContextFactory);
        }

        private IHandlerAgent BuildMessageHandler(object handler, Type contractType)
        {
            var method = MessageHandlerProvider.Instance.GetCachedHandleMethodInfo(contractType, () => handler.GetType());
            var pipeline = GetInterceptorPipeline(method);
            return new MessageHandlerAgent(handler, method, pipeline);
        }

        private IHandlerAgent GetHandlerAgent(Type type)
        {
            Type contractType;
            var handlers = MessageHandlerProvider.Instance.GetCommandHandlers(type, out contractType)
                .Select(handler => BuildCommandHandler(handler, contractType))
                .ToArray();

            if (handlers.IsEmpty()) {
                handlers = MessageHandlerProvider.Instance.GetMessageHandlers(type, out contractType)
                    .Select(handler => BuildMessageHandler(handler, contractType))
                    .ToArray();
            }

            switch (handlers.Length) {
                case 0:
                    throw new MessageHandlerNotFoundException(type);
                case 1:
                    return handlers[0];
                default:
                    throw new MessageHandlerTooManyException(type);
            }
        }

        /// <summary>
        /// 构造命令处理程序
        /// </summary>
        protected override IEnumerable<IHandlerAgent> BuildHandlerAgents(Type type)
        {
            var handler = this.GetHandlerAgent(type);
            var lifecycle = LifeCycleAttribute.GetLifecycle(handler.ReflectedMethod.DeclaringType);
            if (lifecycle == Lifecycle.Singleton)
                AddCachedHandler(type.FullName, handler);

            yield return handler;
        }
    }
}
