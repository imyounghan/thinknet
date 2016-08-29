using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ThinkNet.Messaging
{
    public abstract class EnvelopeHub : DisposableObject, IEnvelopeSender, IEnvelopeReceiver
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphores;
        private readonly BlockingCollection<Envelope>[] brokers;

        private CancellationTokenSource cancellationSource;

        protected EnvelopeHub(int queueCount)
        {
            this.semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            this.brokers = new BlockingCollection<Envelope>[queueCount];

            for(int i = 0; i < queueCount; i++) {
                brokers[i] = new BlockingCollection<Envelope>();
            }
            
        }

        private BlockingCollection<Envelope> GetBroker(string routingKey)
        {
            if(brokers.Length == 1) {
                return brokers[0];
            }

            if(string.IsNullOrWhiteSpace(routingKey)) {
                return brokers.OrderBy(broker => broker.Count).First();
            }

            var index = Math.Abs(routingKey.GetHashCode() % brokers.Length);
            return brokers[index];
        }

        protected void Distribute(Envelope envelope)
        {
            semaphores.GetOrAdd(envelope.Kind, (key) => new SemaphoreSlim(1000, 1000)).Wait();

            GetBroker(envelope.RoutingKey).Add(envelope);
        }

        public event EventHandler<Envelope> EnvelopeReceived = (sender, args) => { };


        private void ReceiveMessages(BlockingCollection<Envelope> queue, CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested) {
                var item = queue.Take();

                this.EnvelopeReceived(this, item);

                semaphores[item.Kind].Release();
            }
        }

        public void Start()
        {
            if(this.cancellationSource == null) {
                this.cancellationSource = new CancellationTokenSource();

                foreach(var broker in brokers) {
                    Task.Factory.StartNew(
                        () => this.ReceiveMessages(broker, this.cancellationSource.Token),
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Current);
                }
            }
        }

        public void Stop()
        {
            using(this.cancellationSource) {
                if(this.cancellationSource != null) {
                    this.cancellationSource.Cancel();
                    this.cancellationSource = null;
                }
            }
        }

        public abstract Task SendAsync(Envelope envelope);

        public abstract Task SendAsync(IEnumerable<Envelope> envelopes);
    }
}
