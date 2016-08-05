using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;


namespace ThinkNet.Messaging.Processing
{
    public abstract class MessageProcessor<T> : Processor
        where T : IMessage
    {
        private readonly BlockingCollection<Envelope<T>>[] brokers;
        private readonly IEnvelopeDelivery envelopeDelivery;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected MessageProcessor(IEnvelopeDelivery envelopeDelivery)
        {
            this.envelopeDelivery = envelopeDelivery;

            var count = ConfigurationSetting.Current.ProcessorCount;
            this.brokers = new BlockingCollection<Envelope<T>>[count];

            this.BuildWorker<Envelope<T>>(EnvelopeBuffer<T>.Instance.Dequeue, Distribute);
            for (int i = 0; i < count; i++) {
                brokers[i] = new BlockingCollection<Envelope<T>>();
                this.BuildWorker<Envelope<T>>(brokers[i].Take, Process);
            }
        }

        protected abstract void Execute(T message);

        protected virtual void Notify(T message, Exception exception)
        { }

        void Process(Envelope<T> item)
        {
            int count = 0;
            int retryTimes = ConfigurationSetting.Current.HandleRetrytimes;


            Exception exception = null;
            while (count++ < retryTimes) {
                try {
                    var sw = Stopwatch.StartNew();
                    this.Execute(item.Body);
                    sw.Stop();
                    item.ProcessingTime = sw.Elapsed;
                    break;
                }
                catch (ThinkNetException ex) {
                    exception = ex;
                    break;
                }
                catch (Exception ex) {
                    if (count == retryTimes) {
                        exception = ex;
                        break;
                    }

                    if (LogManager.Default.IsWarnEnabled) {
                        LogManager.Default.Warn(ex,
                            "An exception happened while processing '{0}'({1}) through handler, Error will be ignored and retry again({2}).",
                            item.Body.GetType().FullName, item.Body, count);
                    }                    
                    Thread.Sleep(ConfigurationSetting.Current.HandleRetryInterval);
                }
            }

            envelopeDelivery.Post(item);
            if (exception != null) {
                if (LogManager.Default.IsErrorEnabled) {
                    LogManager.Default.ErrorFormat("Exception raised when handling '{0}'({1})",
                        item.Body.GetType().FullName, item.Body);
                }
            }
            else {
                if (LogManager.Default.IsDebugEnabled) {
                    LogManager.Default.DebugFormat("Handle '{0}'({1}) success.",
                        item.Body.GetType().FullName, item.Body);
                }
            }

            this.Notify(item.Body, exception);
            
        }


        protected virtual string GetRoutingKey(T data)
        {
            return string.Empty;
        }

        private void Distribute(Envelope<T> message)
        {
            if (message == null || message.Body == null)
                return;

            var routingKey = this.GetRoutingKey(message.Body);
            this.GetBroker(routingKey).Add(message);
        }

        private BlockingCollection<Envelope<T>> GetBroker(string routingKey)
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
    }
}
