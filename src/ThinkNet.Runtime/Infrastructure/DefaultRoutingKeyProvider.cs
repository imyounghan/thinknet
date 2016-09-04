using ThinkNet.Messaging;

namespace ThinkNet.Infrastructure
{
    internal class DefaultRoutingKeyProvider : IRoutingKeyProvider
    {

        #region IRoutingKeyProvider 成员

        public string GetRoutingKey(object payload)
        {
            var reply = payload as RepliedCommand;
            if(reply != null) {
                return reply.CommandId;
            }

            var command = payload as ICommand;
            if (command != null) {
                return command.AggregateRootId;
            }

            var @event = payload as IEvent;
            if (@event != null) {
                return @event.SourceId;
            }

            return string.Empty;
        }

        #endregion
    }
}
