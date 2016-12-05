using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Domain;
using ThinkNet.Infrastructure;
using ThinkNet.Infrastructure.Interception;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;
using ThinkNet.Messaging.Handling.Agent;

namespace ThinkNet.Runtime.Dispatching
{
    /// <summary>
    /// 处理命令的代理程序
    /// </summary>
    public class CommandDispatcher : Dispatcher
    {
        private readonly Func<CommandContext> _commandContextFactory;
        private readonly IInterceptorProvider _interceptorProvider;
        private readonly IMessageBus _messageBus;
        private readonly IMessageHandlerRecordStore _handlerStore;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CommandDispatcher(IObjectContainer container,
            IRepository repository, 
            IEventSourcedRepository eventSourcedRepository,
            IMessageBus messageBus, 
            IMessageHandlerRecordStore handlerStore,
            IInterceptorProvider interceptorProvider)
            : base(container)
        {
            this._commandContextFactory = () => new CommandContext(repository, eventSourcedRepository, messageBus);
            this._interceptorProvider = interceptorProvider;
            this._messageBus = messageBus;
            this._handlerStore = handlerStore;
        }        
        

        private IHandlerAgent GetHandlerAgent(Type commandType)
        {
            var contractType = typeof(ICommandHandler<>).MakeGenericType(commandType);
            var handlers = this.GetMessageHandlers(contractType);

            IHandlerAgent[] handlerAgents = new IHandlerAgent[0];
            if(!handlers.IsEmpty()) {
                //var handlerAgentType = typeof(CommandHandlerAgent<>).MakeGenericType(commandType);
                //var constructor = handlerAgentType.GetConstructors().Single();
                //handlerAgents = handlers.Select(handler => constructor.Invoke(new object[] { handler, _commandContextFactory, _interceptorProvider, _firstInterceptors, _lastInterceptors })).Cast<IHandlerAgent>().ToArray();
                handlerAgents = handlers.Select(handler => new CommandHandlerAgent(contractType, handler, _commandContextFactory, _messageBus, _handlerStore, _interceptorProvider)).Cast<IHandlerAgent>().ToArray();
            }

            if(handlerAgents.Length == 0) {
                contractType = typeof(IMessageHandler<>).MakeGenericType(commandType);
                handlers = this.GetMessageHandlers(contractType);
                if(!handlers.IsEmpty()) {
                    //var handlerAgentType = typeof(MessageHandlerAgent<>).MakeGenericType(commandType);
                    //var constructor = handlerAgentType.GetConstructor(new Type[] { contractType, typeof(IInterceptorProvider), typeof(IEnumerable<IInterceptor>), typeof(IEnumerable<IInterceptor>) });
                    //handlerAgents = handlers.Select(handler => constructor.Invoke(new object[] { handler, _interceptorProvider, _firstInterceptors, _lastInterceptors })).Cast<IHandlerAgent>().ToArray();
                    handlerAgents = handlers.Select(handler => new CommandHandlerAgent(contractType, handler, null, _messageBus, _handlerStore, _interceptorProvider)).Cast<IHandlerAgent>().ToArray();
                }
            }

            switch(handlerAgents.Length) {
                case 0:
                    throw new MessageHandlerNotFoundException(commandType);
                case 1:
                    return handlerAgents[0];
                default:
                    throw new MessageHandlerTooManyException(commandType);
            }
        }

        /// <summary>
        /// 构造命令处理程序
        /// </summary>
        protected override IEnumerable<IHandlerAgent> BuildHandlerAgents(Type type)
        {
            var handler = this.GetHandlerAgent(type);
            var lifecycle = LifeCycleAttribute.GetLifecycle(handler.GetInnerHandler().GetType());
            if (lifecycle == Lifecycle.Singleton)
                AddCachedHandler(type.FullName, handler);

            yield return handler;
        }
    }
}
