using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkNet.Common;
using ThinkNet.Common.Composition;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Interception.Pipeline;
using ThinkNet.Domain;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Proxies;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 处理命令的代理程序
    /// </summary>
    public class CommandDispatcher : MessageDispatcher
    {
        private readonly Func<CommandContext> _commandContextFactory;
        private readonly IInterceptor _firstInterceptor;
        private readonly IInterceptorProvider _interceptorProvider;
        private readonly IHandlerMethodProvider _handlerMethodProvider;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CommandDispatcher(IRepository repository, 
            IEventSourcedRepository eventSourcedRepository,
            IMessageBus messageBus, 
            IHandlerRecordStore handlerStore,
            IInterceptorProvider interceptorProvider,
            IHandlerMethodProvider handlerMethodProvider)
        {
            this._commandContextFactory = () => new CommandContext(repository, eventSourcedRepository, messageBus);
            this._firstInterceptor = new FilterHandledMessageInterceptor(handlerStore);
            this._interceptorProvider = interceptorProvider;
            this._handlerMethodProvider = handlerMethodProvider;
        }

        private InterceptorPipeline GetInterceptorPipeline(MethodInfo method)
        {
            Func<IEnumerable<IInterceptor>> getInterceptors = delegate {
                var interceptors = _interceptorProvider.GetInterceptors(method);
                return new[] { _firstInterceptor }.Concat(interceptors);
            };

            return InterceptorPipelineManager.Instance.CreatePipeline(method, getInterceptors);
        }

        private IHandlerProxy BuildCommandHandler(object handler, Type contractType)
        {
            var method = _handlerMethodProvider.GetCachedMethodInfo(handler.GetType(), contractType);
            var pipeline = GetInterceptorPipeline(method);
            var commandContext = _commandContextFactory.Invoke();
            return new CommandHandlerProxy(handler, method, pipeline, commandContext);
        }

        private IHandlerProxy BuildMessageHandler(object handler, Type contractType)
        {
            var method = _handlerMethodProvider.GetCachedMethodInfo(handler.GetType(), contractType);
            var pipeline = GetInterceptorPipeline(method);
            return new MessageHandlerProxy(handler, method, pipeline);
        }

        private IHandlerProxy GetProxyHandler(Type type)
        {
            var contractType = typeof(ICommandHandler<>).MakeGenericType(type);

            var handlers = ObjectContainer.Instance.ResolveAll(contractType)
                .Select(handler => BuildCommandHandler(handler, contractType))
                .ToArray();

            if (handlers.IsEmpty()) {
                contractType = typeof(IMessageHandler<>).MakeGenericType(type);
                handlers = ObjectContainer.Instance.ResolveAll(contractType)
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
        /// 获取命令处理程序
        /// </summary>
        protected override IEnumerable<IHandlerProxy> GetProxyHandlers(Type type)
        {
            var handler = GetCachedHandler(type.FullName);
            if (handler == null) {
                handler = GetProxyHandler(type);
                var lifecycle = LifeCycleAttribute.GetLifecycle(handler.ReflectedMethod.DeclaringType);
                if (lifecycle == Lifecycle.Singleton)
                    AddHandler(type.FullName, handler);
            }

            yield return handler;
        }

        

        //protected override void OnExecuting(ICommand command, Type handlerType)
        //{
        //    var commandType = command.GetType();
        //    if (_handlerStore.HandlerIsExecuted(command.Id, commandType, handlerType)) {
        //        var errorMessage = string.Format("The command has been handled. CommandHandlerType:{0}, CommandType:{1}, CommandId:{2}.",
        //            handlerType.FullName, commandType.FullName, command.Id);
        //        throw new MessageHandlerProcessedException(errorMessage);
        //    }
        //}

        //protected override void OnExecuted(ICommand command, Type handlerType, Exception ex)
        //{
        //    if (ex != null)
        //        _handlerStore.AddHandlerInfo(command.Id, command.GetType(), handlerType);
        //}
    }
}
