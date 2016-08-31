using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThinkNet.Configurations;

namespace ThinkNet.Messaging
{
    public class EnvelopeHub : DisposableObject, IEnvelopeSender, IEnvelopeReceiver
    {
        //private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphores;
        private readonly BlockingCollection<Envelope>[] brokers;

        private CancellationTokenSource cancellationSource;

        [ImportingConstructor]
        public EnvelopeHub()
            : this(ConfigurationSetting.Current.QueueCount, ConfigurationSetting.Current.QueueCapacity)
        { }

        protected EnvelopeHub(int queueCount, int queueCapacity)
        {
            this.brokers = new BlockingCollection<Envelope>[queueCount];

            for(int i = 0; i < queueCount; i++) {
                brokers[i] = new BlockingCollection<Envelope>(queueCapacity);
            }
        }

        protected override void Dispose(bool disposing)
        { }

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
            GetBroker(envelope.RoutingKey).Add(envelope);
        }

        public event EventHandler<Envelope> EnvelopeReceived = (sender, args) => { };

        private void ReceiveMessages(object state)
        {
            var broker = state as BlockingCollection<Envelope>;
            broker.NotNull("state");

            while (!cancellationSource.IsCancellationRequested) {
                var item = broker.Take();
                this.EnvelopeReceived(this, item);
            }
        }

        void IEnvelopeReceiver.Start()
        {
            if(this.cancellationSource == null) {
                this.cancellationSource = new CancellationTokenSource();

                foreach(var broker in brokers) {
                    Task.Factory.StartNew(this.ReceiveMessages,
                        broker,
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness,
                        TaskScheduler.Current);
                }
            }
        }

        void IEnvelopeReceiver.Stop()
        {
            if (this.cancellationSource != null) {
                using (this.cancellationSource) {
                    this.cancellationSource.Cancel();
                    this.cancellationSource = null;
                }
            }
        }

        public virtual Task SendAsync(Envelope envelope)
        {
            return Task.Factory.StartNew(() => this.Distribute(envelope));
        }

        public virtual Task SendAsync(IEnumerable<Envelope> envelopes)
        {
            return Task.Factory.StartNew(() => envelopes.ForEach(this.Distribute));
        }
    }
}
