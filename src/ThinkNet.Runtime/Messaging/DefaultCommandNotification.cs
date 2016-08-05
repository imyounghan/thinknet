using System;

namespace ThinkNet.Messaging
{
    internal class DefaultCommandNotification : CommandResultManager, ICommandNotification
    {
        public DefaultCommandNotification(ICommandBus commandBus)
            : base(commandBus)
        { }

        #region IMessageNotification 成员

        public void NotifyCompleted(string messageId, Exception exception = null)
        {
            this.NotifyCommandCompleted(messageId,
                exception == null ? CommandStatus.Success : CommandStatus.Failed,
                exception);
        }

        public void NotifyHandled(string messageId, Exception exception = null)
        {
            this.NotifyCommandExecuted(messageId,
                exception == null ? CommandStatus.Success : CommandStatus.Failed,
                exception);
        }

        public void NotifyUnchanged(string messageId)
        {
            this.NotifyCommandCompleted(messageId, CommandStatus.NothingChanged, null);
        }

        #endregion
    }
}
