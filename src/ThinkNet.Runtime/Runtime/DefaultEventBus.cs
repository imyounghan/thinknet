using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    internal class DefaultEventBus : AbstractBus, IEventBus
    {
        private readonly IRoutingKeyProvider routingKeyProvider;
        public DefaultEventBus(IRoutingKeyProvider routingKeyProvider)
        {
            this.routingKeyProvider = routingKeyProvider;
        }

        protected override bool MatchType(Type type)
        {
            return TypeHelper.IsEvent(type);
        }


        public void Publish(IEvent @event)
        {
            this.Publish(new[] { @event });
        }

        public void Publish(IEnumerable<IEvent> events)
        {
            events.Select(Serialize).ForEach(MessageCenter<IEvent>.Instance.Add);
        }

        private Message<IEvent> Serialize(IEvent @event)
        {
            return new Message<IEvent> {
                Body = @event,
                RoutingKey = routingKeyProvider.GetRoutingKey(@event)
            };
        }
    }
}