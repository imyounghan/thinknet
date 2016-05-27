using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    internal class DefaultRoutingKeyProvider : IRoutingKeyProvider
    {

        #region IRoutingKeyProvider 成员

        public string GetRoutingKey(object payload)
        {
            var command = payload as Command;
            if (command != null) {
                return command.GetAggregateRootStringId();
            }

            var @event = payload as Event;
            if (@event != null) {
                return @event.GetSourceStringId();
            }

            return string.Empty;
        }

        #endregion
    }
}
