using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThinkNet.Messaging
{
    public class EnvelopeHub //: IEnvelopeSender, IEnvelopeReceiver
    {
        private readonly SemaphoreSlim semaphore;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> semaphores;
        private readonly BlockingCollection<Envelope>[] brokers;

        private readonly object lockObject = new object();
        private CancellationTokenSource cancellationSource;

        public EnvelopeHub(int queueCount)
        {
            this.semaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            //var count = ConfigurationSetting.Current.ProcessorCount;
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

        //public event EventHandler<Envelope> MessageCompleted = (sender, args) => { };


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
            lock(this.lockObject) {
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
        }

        public void Stop()
        {
            lock(this.lockObject) {
                using(this.cancellationSource) {
                    if(this.cancellationSource != null) {
                        this.cancellationSource.Cancel();
                        this.cancellationSource = null;
                    }
                }
            }
        }

        //#region IEnvelopeSender 成员

        //void IEnvelopeSender.Send(Envelope envelope)
        //{
        //    this.Distribute(envelope);
        //}

        //void IEnvelopeSender.Send(IEnumerable<Envelope> envelopes)
        //{
        //    envelopes.ForEach(this.Distribute);
        //}

        //#endregion
    }
}
