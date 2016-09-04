using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class EnvelopeHub : DisposableObject, IEnvelopeSender, IEnvelopeReceiver
    {
        //private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphores;
        private readonly BlockingCollection<Envelope>[] brokers;
        private readonly IRoutingKeyProvider _routingKeyProvider;

        private CancellationTokenSource cancellationSource;

        [ImportingConstructor]
        public EnvelopeHub(IRoutingKeyProvider routingKeyProvider)
            : this(routingKeyProvider, ConfigurationSetting.Current.QueueCount, ConfigurationSetting.Current.QueueCapacity)
        { }

        protected EnvelopeHub(IRoutingKeyProvider routingKeyProvider, int queueCount, int queueCapacity)
        {
            this._routingKeyProvider = routingKeyProvider;
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

        protected virtual string GetKey(Envelope envelope)
        {
            return envelope.GetMetadata(StandardMetadata.SourceId)
                .IfEmpty(() => _routingKeyProvider.GetRoutingKey(envelope.Body));
        }

        protected void Distribute(Envelope envelope)
        {
            this.GetBroker(this.GetKey(envelope)).Add(envelope);
        }

        public event EventHandler<Envelope> EnvelopeReceived = (sender, args) => { };

        private void ReceiveMessages(object state)
        {
            var broker = state as BlockingCollection<Envelope>;
            broker.NotNull("state");

            //while (!cancellationSource.IsCancellationRequested) {
            //    var item = broker.Take();
            //    this.EnvelopeReceived(this, item);
            //}
            foreach (var item in broker.GetConsumingEnumerable()) {
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
