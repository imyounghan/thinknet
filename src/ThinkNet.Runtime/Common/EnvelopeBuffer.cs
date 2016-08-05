using System;
using System.Collections.Concurrent;
using System.Threading;
using ThinkNet.Configurations;

namespace ThinkNet.Infrastructure
{
    public class EnvelopeBuffer<T> //where T : IMessage
    {
        public readonly static EnvelopeBuffer<T> Instance = new EnvelopeBuffer<T>();

        private readonly ConcurrentQueue<Envelope<T>> queue;
        private readonly ConcurrentDictionary<string, DateTime> wait;
        private readonly SemaphoreSlim producerSemaphore;
        private readonly SemaphoreSlim consumerSemaphore;


        private EnvelopeBuffer()
        {
            int capacity = ConfigurationSetting.Current.QueueCapacity;

            this.queue = new ConcurrentQueue<Envelope<T>>();
            this.wait = new ConcurrentDictionary<string, DateTime>();
            this.producerSemaphore = new SemaphoreSlim(capacity, capacity);
            this.consumerSemaphore = new SemaphoreSlim(0, capacity);
        }

        //public void Deliver(T item)
        //{
        //    this.Enqueue(new Envelope<T>(item));
        //}
        //public T Dequeue()
        //{
        //    return this.Dequeue().Body;
        //}

        //public bool TryEnqueue(IEnumerable<Envelope<T>> items)
        //{
        //    if (producerSemaphore.CurrentCount < items.Count())
        //        return false;
        //}

        public void Enqueue(Envelope<T> item)
        {
            producerSemaphore.Wait();

            queue.Enqueue(item);
            wait.TryAdd(item.CorrelationId, DateTime.UtcNow);
            //item.TimeOfTaked = DateTime.UtcNow;
            //wait.GetOrAdd(item.CorrelationId, key => new EnvelopeTimer()).TimeToLive = DateTime.UtcNow - item.TimeOfCreated;

            consumerSemaphore.Release();
        }

        public Envelope<T> Dequeue()
        {
            consumerSemaphore.Wait();

            var item = queue.Dequeue();
            if (item != null && item.Body != null) {
                item.Delay = DateTime.UtcNow - wait.Remove(item.CorrelationId);
            }

            return item;
        }

        public void Complete(Envelope<T> item)
        {
            producerSemaphore.Release();

            this.EnvelopeCompleted(item);
        }

        public event Action<Envelope<T>> EnvelopeCompleted = (arg) => { };
    }
}
