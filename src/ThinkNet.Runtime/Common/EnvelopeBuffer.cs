using System;
using System.Collections.Concurrent;
using System.Threading;
using ThinkNet.Configurations;
using ThinkNet.Messaging;

namespace ThinkNet.Common
{
    public class EnvelopeBuffer<T>// where T : IMessage
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

        public void Enqueue(Envelope<T> item)
        {
            var time = wait.GetOrAdd(item.CorrelationId, DateTime.UtcNow);
            producerSemaphore.Wait();

            item.WaitTime = DateTime.UtcNow - time;
            queue.Enqueue(item);
            wait.TryUpdate(item.CorrelationId, DateTime.UtcNow, time);

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
