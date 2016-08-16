using System;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class CommandNotification : KafkaBus, ICommandNotification
    {
        public CommandNotification(ISerializer serializer, IMetadataProvider metadataProvider, ITopicProvider topicProvider)
            : base(serializer, metadataProvider, topicProvider)
        { }

        public void NotifyCompleted(string messageId, Exception exception = null)
        {
            base.Push(new[] { new CommandReply(messageId, exception, CommandResultType.DomainEventHandled) });
        }

        public void NotifyHandled(string messageId, Exception exception = null)
        {
            base.Push(new[] { new CommandReply(messageId, exception, CommandResultType.CommandExecuted) });
        }

        public void NotifyUnchanged(string messageId)
        {
            base.Push(new[] { new CommandReply(messageId) });
        }

        protected override bool MatchType(Type type)
        {
            return false;
        }
    }
}
