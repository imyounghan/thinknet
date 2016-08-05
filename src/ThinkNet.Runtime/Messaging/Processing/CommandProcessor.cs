using System;
using ThinkNet.Common;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class CommandProcessor : MessageProcessor<ICommand>
    {
        private readonly ICommandNotification _notification;
        private readonly IHandlerProvider _handlerProvider;
        private readonly IRoutingKeyProvider _routingKeyProvider;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CommandProcessor(ICommandNotification notification,
            IHandlerProvider handlerProvider,
            IRoutingKeyProvider routingKeyProvider, 
            IEnvelopeDelivery envelopeDelivery)
            : base(envelopeDelivery)
        {
            this._notification = notification;
            this._handlerProvider = handlerProvider;
            this._routingKeyProvider = routingKeyProvider;
        }

        protected override string GetRoutingKey(ICommand data)
        {
            return _routingKeyProvider.GetRoutingKey(data);
        }

        protected override void Execute(ICommand command)
        {
            var commandType = command.GetType();

            var handler = _handlerProvider.GetCommandHandler(commandType);
            handler.Handle(command);
            //OnlyonceProcessHandler(handler, command, commandType);
        }

        protected override void Notify(ICommand command, Exception exception)
        {
            _notification.NotifyHandled(command.Id, exception);
        }  
    }
}
