using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class EventBus : AbstractBus, IEventBus
    {
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
            KafkaClient.Instance.Push(events);
        }

    }
}
