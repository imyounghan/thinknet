using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ThinkNet.Common.Composition;
using ThinkNet.Common.Interception;
using ThinkNet.Common.Interception.Pipeline;
using ThinkNet.Domain;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Proxies;

namespace ThinkNet.Runtime.Executing
{

    public class CommandExecutor : Executor
    {
        //private readonly IMessageHandlerRecordStore _handlerStore;
        private readonly Func<CommandContext> _commandContextFactory;
        private readonly IInterceptor _firstInterceptor;
        public CommandExecutor(IRepository repository, 
            IEventSourcedRepository eventSourcedRepository,
            IMessageBus messageBus, 
            IMessageHandlerRecordStore handlerStore)
        {
            this._commandContextFactory = () => new CommandContext(repository, eventSourcedRepository, messageBus);
            //this._handlerStore = handlerStore;
            this._firstInterceptor = new MessageHandledInterceptor(handlerStore);
        }


        //protected override IEnumerable<IHandler> GetHandlers(Type type)
        //{
        //    var contractType = typeof(ICommandHandler<>).MakeGenericType(type);

        //    var handlers = ObjectContainer.Instance.ResolveAll(contractType).Cast<IHandler>().ToArray();
        //    if (handlers.Length == 0) {
        //        handlers = base.GetHandlers(type).ToArray();
        //    }

        //    switch (handlers.Length) {
        //        case 0:
        //            throw new MessageHandlerNotFoundException(type);
        //        case 1:
        //            yield return handlers[0];
        //            break;
        //        default:
        //            throw new MessageHandlerTooManyException(type);
        //    }
        //}

        protected override IEnumerable<IInterceptor> GetInterceptors(MethodInfo method)
        {
            var interceptors = base.GetInterceptors(method);

            return new[] { _firstInterceptor }.Concat(interceptors);
        }

        protected override IEnumerable<IProxyHandler> GetProxyHandlers(Type type)
        {
            var contractType = typeof(ICommandHandler<>).MakeGenericType(type);

            var handlers = ObjectContainer.Instance.ResolveAll(contractType)
                .Cast<IHandler>()
                .Select(handler => {
                    InterceptorPipeline pipeline;
                    var method = base.GetCachedHandleMethodInfo(contractType, () => handler.GetType(), out pipeline);
                    var commandContext = _commandContextFactory.Invoke();
                    return new CommandHandlerProxy(handler, method, pipeline, commandContext);
                })
                .Cast<IProxyHandler>()
                .ToArray();

            if (handlers.IsEmpty()) {
                contractType = typeof(IMessageHandler<>).MakeGenericType(type);
                handlers = ObjectContainer.Instance.ResolveAll(contractType).Cast<IHandler>()
                    .Select(handler => {
                        InterceptorPipeline pipeline;
                        var method = base.GetCachedHandleMethodInfo(contractType, () => handler.GetType(), out pipeline);
                        return new MessageHandlerProxy(handler, method, pipeline);
                    })
                    .Cast<IProxyHandler>()
                    .ToArray();
            }

            switch (handlers.Length) {
                case 0:
                    throw new MessageHandlerNotFoundException(type);
                case 1:
                    yield return handlers[0];
                    break;
                default:
                    throw new MessageHandlerTooManyException(type);
            }
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
