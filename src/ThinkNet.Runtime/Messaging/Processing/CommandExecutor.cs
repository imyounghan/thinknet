using System;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class CommandExecutor : MessageExecutor<ICommand>
    {
        private readonly IEnvelopeSender _sender;
        private readonly IHandlerProvider _handlerProvider;

        public CommandExecutor(IEnvelopeSender sender, IHandlerProvider handlerProvider)
        {
            this._sender = sender;
            this._handlerProvider = handlerProvider;
        }

        protected override void Execute(ICommand command)
        {
            var commandType = command.GetType();

            var handler = _handlerProvider.GetCommandHandler(commandType);
            handler.Handle(command);
        }

        protected override void OnExecuted(ICommand command)
        {
            _sender.SendAsync(Transform(command, null));
            base.OnExecuted(command);
        }

        protected override void OnException(ICommand command, Exception ex)
        {
            _sender.SendAsync(Transform(command, ex));
            base.OnException(command, ex);
        }

        private Envelope Transform(ICommand command, Exception ex)
        {
            var reply = new CommandReply(command.Id, ex, CommandReturnType.CommandExecuted);

            return new Envelope() {
                Body = reply,
                CorrelationId = reply.Id,
                RoutingKey = command.Id,
            };
        }

        //protected override void Notify(ICommand command, Exception exception)
        //{
        //    _notification.NotifyHandled(command.Id, exception);
        //    base.Notify(command, exception);
        //}
    }
}
