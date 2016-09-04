using System;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class CommandExecutor : MessageExecutor<ICommand>
    {
        private readonly IEnvelopeSender _sender;
        private readonly IHandlerProvider _handlerProvider;
        private readonly IHandlerRecordStore _handlerStore;

        public CommandExecutor(IEnvelopeSender sender, IHandlerProvider handlerProvider, IHandlerRecordStore handlerStore)
        {
            this._sender = sender;
            this._handlerProvider = handlerProvider;
            this._handlerStore = handlerStore;
        }

        protected override ExecutionStatus Execute(ICommand command)
        {
            var commandType = command.GetType();
            var handler = _handlerProvider.GetCommandHandler(commandType);

            if(_handlerStore.HandlerIsExecuted(command.Id, commandType, handler.HanderType)) {
                handler.Handle(command);
                return ExecutionStatus.Obsoleted;
            }

            _handlerStore.AddHandlerInfo(command.Id, commandType, handler.HanderType);

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
            var reply = new RepliedCommand(command.Id, ex, CommandReturnType.CommandExecuted);
            var envelope = new Envelope(reply);
            envelope.Metadata[StandardMetadata.IdentifierId] = reply.Id;
            envelope.Metadata[StandardMetadata.SourceId] = reply.CommandId;
            envelope.Metadata[StandardMetadata.Kind] = StandardMetadata.RepliedCommandKind;

            return envelope;
        }

        //protected override void Notify(ICommand command, Exception exception)
        //{
        //    _notification.NotifyHandled(command.Id, exception);
        //    base.Notify(command, exception);
        //}
    }
}
