using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ThinkNet.Runtime.Routing
{
    /// <summary>
    /// <see cref="IEnvelopeSender"/> 和 <see cref="IEnvelopeReceiver"/> 的实现类
    /// </summary>
    public class EnvelopeHub : IEnvelopeSender, IEnvelopeReceiver
    {
        private readonly BlockingCollection<Envelope>[] brokers;
        private readonly IRoutingKeyProvider _routingKeyProvider;

        private CancellationTokenSource cancellationSource;
        private int counter; 

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>        
        public EnvelopeHub(IRoutingKeyProvider routingKeyProvider)
            : this(routingKeyProvider, ConfigurationSetting.Current.QueueCount)
        { }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected EnvelopeHub(IRoutingKeyProvider routingKeyProvider, int queueCount)
        {
            this._routingKeyProvider = routingKeyProvider;
            this.brokers = new BlockingCollection<Envelope>[queueCount];

            for(int i = 0; i < queueCount; i++) {
                this.brokers[i] = new BlockingCollection<Envelope>();
            }
        }

        private BlockingCollection<Envelope> GetBroker(string routingKey, out int index)
        {
            var processorCount = this.brokers.Length;

            if(processorCount == 1) {
                index = 0;
                return this.brokers[0];
            }

            if(string.IsNullOrWhiteSpace(routingKey)) {
                index = Math.Abs(Interlocked.Increment(ref counter) % processorCount);
                //return this.brokers.OrderBy(broker => broker.Count).First();
            }
            else {
                index = Math.Abs(routingKey.GetHashCode() % processorCount);
            }
            
            return this.brokers[index];
        }

        /// <summary>
        /// 获取关键字
        /// </summary>
        protected virtual string GetKey(Envelope envelope)
        {
            return envelope.GetMetadata(StandardMetadata.SourceId)
                .IfEmpty(() => _routingKeyProvider.GetRoutingKey(envelope.Body));
        }

        /// <summary>
        /// 收到信件后的处理方式
        /// </summary>
        public event EventHandler<Envelope> EnvelopeReceived = (sender, args) => { };

        private void ReceiveMessages(object state)
        {
            var tuple = state as Tuple<int, BlockingCollection<Envelope>>;
            var position = tuple.Item1;
            var broker = tuple.Item2;

            //while(!cancellationSource.IsCancellationRequested) {
            //    var item = broker.Take(cancellationSource.Token);
            //    this.EnvelopeReceived(this, item);
            //}
            foreach(var item in broker.GetConsumingEnumerable(cancellationSource.Token)) {
                if(LogManager.Default.IsDebugEnabled) {
                    LogManager.Default.DebugFormat("Receive an envelope from local queue({0}), data:({1}).",
                        position, item.Body);
                }

                item.Items["ExitQueueTime"] = DateTime.UtcNow;
                this.EnvelopeReceived(this, item);
            }
        }

        void IEnvelopeReceiver.Start()
        {
            if(this.cancellationSource == null) {
                this.cancellationSource = new CancellationTokenSource();

                for(int i = 0; i < brokers.Length; i++) {
                    Task.Factory.StartNew(this.ReceiveMessages,
                        Tuple.Create<int, BlockingCollection<Envelope>>(i, brokers[i]),
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

        /// <summary>
        /// Sends an envelope.
        /// </summary>
        public void Send(Envelope envelope)
        {
            int index;
            var broker = this.GetBroker(this.GetKey(envelope), out index);            

            envelope.Items["EntryQueueTime"] = DateTime.UtcNow;
            broker.Add(envelope, this.cancellationSource.Token);

            if(LogManager.Default.IsDebugEnabled) {
                LogManager.Default.DebugFormat("Distribute an envelope({0}) in queue({1}) waited {2}ms.",
                    envelope.Body, index, envelope.WaitTime.TotalMilliseconds);
            }
        }
        ///// <summary>
        ///// Sends a batch of envelopes.
        ///// </summary>
        //public virtual Task SendAsync(IEnumerable<Envelope> envelopes)
        //{
        //    if(LogManager.Default.IsDebugEnabled) {
        //        LogManager.Default.DebugFormat("Send a batch of envelope to local queue, data:(0).", 
        //            string.Join(";", envelopes.Select(item=>item.Body.ToString())));
        //    }

        //    return Task.Factory.StartNew(delegate {
        //        envelopes.ForEach(this.Route);
        //    });
        //}
    }
}
