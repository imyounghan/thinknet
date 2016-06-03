using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using ThinkLib.Scheduling;
using ThinkNet.Configurations;

namespace ThinkNet.Infrastructure
{
    //public class MessageCenter
    //{
    //    private static ConcurrentDictionary<Type, object> dict = new ConcurrentDictionary<Type, object>();

    //    public static MessageCenter<T> Get<T>()
    //    {
    //        return dict.GetOrAdd(typeof(T), (key) => new MessageCenter<T>()) as MessageCenter<T>;
    //    }

    //    internal readonly Worker[] works;

    //    protected MessageCenter(int workerCount)
    //    {
    //        works = new Worker[workerCount];
    //    }
    //}


    public class MessageCenter<T> 
    {
        public readonly static MessageCenter<T> Instance = new MessageCenter<T>();
        


        private readonly BlockingCollection<Message<T>>[] brokers;
        private readonly Worker[] works;

        private MessageCenter()
            : base()
        {
            var count = ConfigurationSetting.Current.QueueCount;
            brokers = new BlockingCollection<Message<T>>[count];
            works = new Worker[count];

            for (int i = 0; i < count; i++) {
                brokers[i] = new BlockingCollection<Message<T>>(ConfigurationSetting.Current.QueueCapacity);
                works[i] = WorkerFactory.Create<Message<T>>(brokers[i].Take, Processing);
            }
        }

        public void Add(Message<T> message)
        {
            this.GetBroker(message.RoutingKey).Add(message);
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
            return this.GetBroker(message.RoutingKey).TryAdd(message);
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

        //public event EventHandler<Message<T>> MessageHandled = (sender, arg) => { };

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
