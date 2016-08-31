using System;

namespace ThinkNet.Messaging.Processing
{
    public class CommandReplyExecutor : MessageExecutor<CommandReply>
    {
        private readonly ICommandNotification _notification;

        public CommandReplyExecutor(ICommandNotification notification)
        {
            this._notification = notification;
        }

        protected override ExecutionStatus Execute(CommandReply reply)
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
