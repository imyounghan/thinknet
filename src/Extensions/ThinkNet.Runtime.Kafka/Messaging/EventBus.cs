using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class EventBus : KafkaBus, IEventBus
    {
        public EventBus(ISerializer serializer, IMetadataProvider metadataProvider, ITopicProvider topicProvider)
            : base(serializer, metadataProvider, topicProvider)
        { }

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

            base.Push(events);
        }

    }
}
