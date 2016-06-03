using System;
using ThinkNet.Messaging;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Runtime
{
    public class CommandProcessor : MessageProcessor<ICommand>
    {
        private readonly IMessageNotification _notification;
        private readonly IHandlerProvider _handlerProvider;
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CommandProcessor(IMessageNotification notification,
            IHandlerRecordStore handlerStore,
            IHandlerProvider handlerProvider)
            : base(handlerStore)
        {
            this._notification = notification;
            this._handlerProvider = handlerProvider;
        }

        protected override void Execute(ICommand command)
        {
            var commandType = command.GetType();

            var handler = _handlerProvider.GetCommandHandler(commandType);
            OnlyonceProcessHandler(handler, command, commandType);
        }

        protected override void Notify(ICommand command, Exception exception)
        {
            _notification.NotifyMessageHandled(command.Id, exception);
        }  
    }
}
