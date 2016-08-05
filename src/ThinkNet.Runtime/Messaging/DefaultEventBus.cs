using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    internal class DefaultEventBus : AbstractBus, IEventBus
    {
        private readonly BlockingCollection<IEvent> queue;
        private readonly Worker worker;

        public DefaultEventBus()
        {
            this.queue = new BlockingCollection<IEvent>();
            this.worker = WorkerFactory.Create<IEvent>(queue.Take, Transfer);
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
            if (queue.Count + events.Count() >= ConfigurationSetting.Current.QueueCapacity * 5)
                throw new Exception("server is busy.");

            events.ForEach(queue.Add);
        }

        private void Transfer(IEvent @event)
        {
            var stream = @event as EventStream;
            if (stream == null) {
                var item = new Envelope<IEvent>(@event) {
                    CorrelationId = @event.Id
                };

                EnvelopeBuffer<IEvent>.Instance.Enqueue(item);
                item.WaitTime = DateTime.UtcNow - @event.CreatedTime;
            }
            else {
                var item = new Envelope<EventStream>(stream) {
                    CorrelationId = @event.Id
                };

                EnvelopeBuffer<EventStream>.Instance.Enqueue(item);
                item.WaitTime = DateTime.UtcNow - @event.CreatedTime;
            }           
        }
    }
}