using ThinkNet.Messaging;

namespace ThinkNet.Infrastructure
{
    internal class DefaultRoutingKeyProvider : IRoutingKeyProvider
    {

        #region IRoutingKeyProvider 成员

        public string GetRoutingKey(object payload)
        {
            var command = payload as ICommand;
            if (command != null) {
                return command.AggregateRootId.IfEmpty(string.Empty);
            }

            var @event = payload as IEvent;
            if (@event != null) {
                return @event.SourceId.IfEmpty(string.Empty);
            }

            return string.Empty;
        }

        #endregion
    }
}
