using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class EventBus : AbstractBus, IEventBus
    {
        private readonly KafkaClient _kafkaClient;

        public EventBus(KafkaClient kafkaClient)
        {
            this._kafkaClient = kafkaClient;
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
            if (events.IsEmpty())
                return;

            _kafkaClient.Push(events);
        }

    }
}
