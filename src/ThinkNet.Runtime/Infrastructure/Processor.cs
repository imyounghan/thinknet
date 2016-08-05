using System.Collections.Concurrent;
using ThinkNet.Configurations;

namespace ThinkNet.Infrastructure
{
    public abstract class Processor<T> : DisposableObject, IProcessor
    {
        private readonly BlockingCollection<T>[] brokers;
        //private readonly Worker[] works;
        private readonly IRoutingKeyProvider routingKeyProvider;
        
        private readonly object lockObject;
        private bool started;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected Processor()
        {
            this.lockObject = new object();

            var count = ConfigurationSetting.Current.ProcessorCount;
            this.brokers = new BlockingCollection<T>[count];
            //this.works = new Worker[count + 1];

            //works[0] = WorkerFactory.Create<T>(EnvelopeBuffer<T>.Instance.Dequeue(), Process);
            for (int i = 0; i < count; i++) {
                brokers[i] = new BlockingCollection<T>();
                //works[i + 1] = WorkerFactory.Create<T>(brokers[i].Take, Process);
            }
        }

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public virtual void Start()
        {
            ThrowIfDisposed();
            lock (this.lockObject) {
                if (!this.started) {
                    //MessageCenter<T>.Instance.MessageHandling += OnMessageHandling;
                    //MessageCenter<T>.Instance.Start();
                    //works.ForEach(StartWorker);
                    this.started = true;
                }
            }
        }

        //private static void StartWorker(Worker worker)
        //{
        //    worker.Start();
        //}

        /// <summary>
        /// Stops the listener.
        /// </summary>
        public virtual void Stop()
        {
            lock (this.lockObject) {
                if (this.started) {
                    //MessageCenter<T>.Instance.Stop();
                    //MessageCenter<T>.Instance.MessageHandling -= OnMessageHandling;
                    //works.ForEach(StopWorker);
                    this.started = false;
                }
            }
        }

        //private static void StopWorker(Worker worker)
        //{
        //    worker.Stop();
        //}
               
        
        /// <summary>
        /// Disposes the resources used by the processor.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            ThrowIfDisposed();

            if (disposing) {
                this.Stop();
            }
        }

        protected abstract void Process(T message);
        

        //private void OnMessageHandling(object sender, Message<T> arg)
        //{
        //    var message = arg.Body;
        //    if (message.IsNull()) {
        //        Console.WriteLine("empty message.");
        //        return;
        //    }

        //    //arg.TimeForWait = DateTime.UtcNow - arg.CreatedTime;

        //    try {
        //        this.Process(message);
        //    }
        //    catch (Exception ex) {
        //        logger.Error("An exception happened while processing message through handler", ex);
        //        logger.Warn("Error will be ignored and message receiving will continue.");
        //    }
        //}
    }
}
