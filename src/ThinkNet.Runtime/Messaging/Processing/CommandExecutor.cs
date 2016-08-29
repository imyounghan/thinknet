using System;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class CommandExecutor : MessageExecutor<ICommand>
    {
        private readonly ICommandNotification _notification;
        private readonly IHandlerProvider _handlerProvider;

        public CommandExecutor(ICommandNotification notification, IHandlerProvider handlerProvider)
        {
            this._notification = notification;
            this._handlerProvider = handlerProvider;
        }

        protected override void Execute(ICommand command)
        {
            var commandType = command.GetType();

            var handler = _handlerProvider.GetCommandHandler(commandType);
            handler.Handle(command);
        }

        protected override void Notify(ICommand command, Exception exception)
        {
            _notification.NotifyHandled(command.Id, exception);
            base.Notify(command, exception);
        }
    }
}
