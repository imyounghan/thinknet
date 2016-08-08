using System;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class CommandNotification : ICommandNotification
    {
        private readonly KafkaClient _kafkaClient;

        public CommandNotification(KafkaClient kafkaClient)
        {
            this._kafkaClient = kafkaClient;
        }

        public void NotifyCompleted(string messageId, Exception exception = null)
        {
            _kafkaClient.Push(new[] { new CommandReply(messageId, exception, CommandResultType.DomainEventHandled) });
        }

        public void NotifyHandled(string messageId, Exception exception = null)
        {
            _kafkaClient.Push(new[] { new CommandReply(messageId, exception, CommandResultType.CommandExecuted) });
        }

        public void NotifyUnchanged(string messageId)
        {
            _kafkaClient.Push(new[] { new CommandReply(messageId) });
        }
    }
}
