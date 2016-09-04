namespace ThinkNet.Messaging.Processing
{
    public class RepliedCommandExecutor : MessageExecutor<RepliedCommand>
    {
        private readonly ICommandNotification _notification;

        public RepliedCommandExecutor(ICommandNotification notification)
        {
            this._notification = notification;
        }

        protected override ExecutionStatus Execute(RepliedCommand reply)
        {
            switch(reply.CommandReturnType) {
                case CommandReturnType.CommandExecuted:
                    //_notification.NotifyHandled(new CommandResult(reply.Status, reply.CommandId, reply.ExceptionTypeName, reply.ErrorMessage, reply.ErrorData));
                    _notification.NotifyHandled(reply.CommandId, reply.GetInnerException());
                    break;
                case CommandReturnType.DomainEventHandled:
                    //_notification.NotifyCommandCompleted(new CommandResult(reply.Status, reply.CommandId, reply.ExceptionTypeName, reply.ErrorMessage, reply.ErrorData));
                    _notification.NotifyCompleted(reply.CommandId, reply.GetInnerException());
                    break;
            }
            return ExecutionStatus.Completed;
        }
    }
}
