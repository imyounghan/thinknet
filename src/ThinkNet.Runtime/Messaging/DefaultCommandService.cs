using System;

namespace ThinkNet.Messaging
{
    internal class DefaultCommandService : CommandService, ICommandNotification
    {
        private readonly ICommandBus _commandBus;

        public DefaultCommandService(ICommandBus commandBus)
        {
            this._commandBus = commandBus;
        }

        public override void Send(ICommand command)
        {
            _commandBus.Send(command);
        }

        #region IMessageNotification 成员

        public void NotifyCompleted(string commandId, Exception exception = null)
        {
            this.NotifyCommandCompleted(commandId,
                exception == null ? CommandStatus.Success : CommandStatus.Failed,
                exception);
        }

        public void NotifyHandled(string commandId, Exception exception = null)
        {
            this.NotifyCommandExecuted(commandId,
                exception == null ? CommandStatus.Success : CommandStatus.Failed,
                exception);
        }

        public void NotifyUnchanged(string commandId)
        {
            this.NotifyCommandCompleted(commandId, CommandStatus.NothingChanged, null);
        }

        #endregion
    }
}
