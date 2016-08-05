using System;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class CommandNotification : ICommandNotification
    {
        public void NotifyCompleted(string messageId, Exception exception = null)
        {
            KafkaClient.Instance.Push(new[] { new CommandReply(messageId, exception, CommandResultType.DomainEventHandled) });
        }

        public void NotifyHandled(string messageId, Exception exception = null)
        {
            KafkaClient.Instance.Push(new[] { new CommandReply(messageId, exception, CommandResultType.CommandExecuted) });
        }

        public void NotifyUnchanged(string messageId)
        {
            KafkaClient.Instance.Push(new[] { new CommandReply(messageId) });
        }
    }
}
