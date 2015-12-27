using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class EventBus : AbstractBus, IEventBus
    {
        private readonly IMessageSender messageSender;

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

        private static MetaMessage Map(IEvent @event)
        {
            return new MetaMessage {
                Body = @event,
                Topic = "Event",
                RoutingKey = @event.GetRoutingKey()
            };
        }
    }
}
