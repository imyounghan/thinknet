using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    internal class DefaultEventBus : AbstractBus, IEventBus
    {
        private readonly BlockingCollection<IEvent> queue;
        private readonly Worker worker;
        private readonly IEnvelopeHub hub;
        private int limit;

        public DefaultEventBus(IEnvelopeHub hub)
        {
            this.limit = ConfigurationSetting.Current.QueueCapacity * 5;
            this.queue = new BlockingCollection<IEvent>();
            this.worker = WorkerFactory.Create<IEvent>(Transform, queue.Take);
            this.hub = hub;
        }

        protected override void Initialize(IEnumerable<Type> types)
        {
            worker.Start();
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

            var count = 0 - events.Count();
            if (Interlocked.Add(ref limit, count) < 0) {
                Interlocked.Add(ref limit, Math.Abs(count));
                throw new Exception("server is busy.");
            }

            events.ForEach(queue.Add);
        }

        private void Transform(IEvent @event)
        {
            Interlocked.Increment(ref limit);
            hub.Distribute(@event);
        }
    }
}