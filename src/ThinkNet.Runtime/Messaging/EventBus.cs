using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class EventBus : AbstractBus, IEventBus
    {
        private readonly IMessageSender messageSender;
        private readonly IRoutingKeyProvider routingKeyProvider;
        private readonly IMetadataProvider metadataProvider;

        public EventBus(IMessageSender messageSender,
            IRoutingKeyProvider routingKeyProvider,
            IMetadataProvider metadataProvider)
        {
            this.messageSender = messageSender;
            this.routingKeyProvider = routingKeyProvider;
            this.metadataProvider = metadataProvider;
        }

        protected override bool SearchMatchType(Type type)
        {
            return TypeHelper.IsEvent(type);
        }


        public void Publish(IEvent @event)
        {
            this.Publish(new[] { @event });
        }

        public void Publish(IEnumerable<IEvent> events)
        {
            var messages = events.Select(Map).AsEnumerable();
            messageSender.Send(messages);
        }

        private Message Map(IEvent @event)
        {
            return new Message {
                Body = @event,
                MetadataInfo = metadataProvider.GetMetadata(@event),
                RoutingKey = routingKeyProvider.GetRoutingKey(@event),
                CreatedTime = DateTime.UtcNow
            };
        }
    }
}
