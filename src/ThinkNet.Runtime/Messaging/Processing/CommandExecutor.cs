using System;
using ThinkNet.Infrastructure;
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

        protected override ExecutionStatus Execute(ICommand command)
        {
            var commandType = command.GetType();

            var handler = _handlerProvider.GetCommandHandler(commandType);
            handler.Handle(command);

            return ExecutionStatus.Completed;
        }


        protected override void OnExecuted(ICommand command, ExecutionStatus status)
        {
            _sender.SendAsync(Transform(command, null));
            base.OnExecuted(command, status);
        }

        protected override void OnException(ICommand command, Exception ex)
        {
            _sender.SendAsync(Transform(command, ex));
            base.OnException(command, ex);
        }

        private Envelope Transform(ICommand command, Exception ex)
        {
            var reply = new CommandReply(command.Id, ex, CommandReturnType.CommandExecuted);
            var envelope = new Envelope(reply);
            envelope.Metadata[StandardMetadata.CorrelationId] = reply.Id;
            envelope.Metadata[StandardMetadata.RoutingKey] = command.Id;
            envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.CommandReplyKind;

            return envelope;
        }

        //protected override void Notify(ICommand command, Exception exception)
        //{
        //    _notification.NotifyHandled(command.Id, exception);
        //    base.Notify(command, exception);
        //}
    }
}
