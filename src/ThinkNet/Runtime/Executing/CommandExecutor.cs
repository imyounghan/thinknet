using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common.Composition;
using ThinkNet.Domain;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Runtime.Executing
{

    public class CommandExecutor : Executor<ICommand>
    {
        private readonly IHandlerRecordStore _handlerStore;
        private readonly Func<CommandContext> _commandContextFactory;
        public CommandExecutor(IRepository repository, 
            IEventSourcedRepository eventSourcedRepository,
            IMessageBus messageBus, 
            IHandlerRecordStore handlerStore)
        {
            this._commandContextFactory = () => new CommandContext(repository, eventSourcedRepository, messageBus);
            this._handlerStore = handlerStore;
        }

        protected override IEnumerable<IProxyHandler> GetHandlers(Type type)
        {
            var contractType = typeof(ICommandHandler<>).MakeGenericType(type);
            var handlers = ObjectContainer.Instance.ResolveAll(contractType)
                .Cast<IHandler>()
                .Select(handler => new CommandHandlerWrapper(handler, contractType, _commandContextFactory.Invoke()))
                .Cast<IProxyHandler>()
                .ToArray();

            if (handlers.IsEmpty()) {
                handlers = base.GetHandlers(type).ToArray();
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

        protected override void OnExecuting(ICommand command, Type handlerType)
        {
            var commandType = command.GetType();
            if (_handlerStore.HandlerIsExecuted(command.Id, commandType, handlerType)) {
                var errorMessage = string.Format("The command has been handled. commandHandlerType:{0}, commandType:{0}, commandId:{1}.",
                    handlerType.FullName, commandType.FullName, command.Id);
                throw new MessageHandlerProcessedException();
            }
        }

        protected override void OnExecuted(ICommand command, Type handlerType, Exception ex)
        {
            if (ex != null)
                _handlerStore.AddHandlerInfo(command.Id, command.GetType(), handlerType);
        }        
    }
}
