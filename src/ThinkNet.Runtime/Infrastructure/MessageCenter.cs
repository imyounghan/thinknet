using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Practices.ServiceLocation;
using ThinkLib.Scheduling;
using ThinkNet.Configurations;

namespace ThinkNet.Infrastructure
{

    public class MessageCenter<T> 
    {
        public readonly static MessageCenter<T> Instance = new MessageCenter<T>();
        


        private readonly BlockingCollection<Message<T>>[] brokers;
        private readonly Worker[] works;
        private readonly IRoutingKeyProvider routingKeyProvider;

        private MessageCenter()
        {
            this.routingKeyProvider = ServiceLocator.Current.GetInstance<IRoutingKeyProvider>();

            var count = ConfigurationSetting.Current.QueueCount;
            this.brokers = new BlockingCollection<Message<T>>[count];
            this.works = new Worker[count];

            for (int i = 0; i < count; i++) {
                brokers[i] = new BlockingCollection<Message<T>>(ConfigurationSetting.Current.QueueCapacity);
                works[i] = WorkerFactory.Create<Message<T>>(brokers[i].Take, Processing);
            }
        }

        public void Add(Message<T> message)
        {
            var routingKey = routingKeyProvider.GetRoutingKey(message.Body);
            this.GetBroker(routingKey).Add(message);
        }

        private BlockingCollection<Message<T>> GetBroker(string routingKey)
        {
            if (brokers.Length == 1) {
                return brokers[0];
            }

            if (string.IsNullOrWhiteSpace(routingKey)) {
                return brokers.OrderBy(broker => broker.Count).First();
            }

            var index = Math.Abs(routingKey.GetHashCode() % brokers.Length);
            return brokers[index];
        }

        public bool TryAdd(Message<T> message)
        {
            var routingKey = routingKeyProvider.GetRoutingKey(message.Body);
            return this.GetBroker(routingKey).TryAdd(message);
        }

        public Message<T> Take()
        { 
            Message<T> message;
            BlockingCollection<Message<T>>.TakeFromAny(brokers, out message);

            return message;
        }

        public bool TryTake(out Message<T> message)
        {
            return BlockingCollection<Message<T>>.TryTakeFromAny(brokers, out message) != -1;
        }

        private void Processing(Message<T> message)
        {
            this.MessageHandling(this, message);
        }

        public event EventHandler<Message<T>> MessageHandling = (sender, arg) => { };

        public void Start()
        {
            foreach (var worker in works) {
                worker.Start();
            }
        }

        public void Stop()
        {
            foreach (var worker in works) {
                worker.Stop();
            }
        }
    }
}
