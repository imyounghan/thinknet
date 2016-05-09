using ThinkLib.Common;
using ThinkNet.Messaging;

namespace ThinkNet.Infrastructure
{
    [UnderlyingComponent(typeof(DefaultRoutingKeyProvider))]
    public interface IRoutingKeyProvider
    {
        string GetRoutingKey(object payload);
    }

    internal class DefaultRoutingKeyProvider : IRoutingKeyProvider
    {

        #region IRoutingKeyProvider 成员

        public string GetRoutingKey(object payload)
        {
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
