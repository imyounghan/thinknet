using System;
using ThinkLib.Common;
using ThinkLib.Logging;

namespace ThinkNet.Infrastructure
{
    public abstract class Processor<T> : DisposableObject, IProcessor
    {
        protected readonly ILogger logger;

        
        private readonly object lockObject = new object();
        private bool started = false;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected Processor()
        {
            this.logger = LogManager.GetLogger("ThinkNet");
        }

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public virtual void Start()
        {
            ThrowIfDisposed();
            lock (this.lockObject) {
                if (!this.started) {
                    MessageCenter<T>.Instance.MessageHandling += OnMessageHandling;
                    MessageCenter<T>.Instance.Start();
                    this.started = true;
                }
            }
        }

        /// <summary>
        /// Stops the listener.
        /// </summary>
        public virtual void Stop()
        {
            lock (this.lockObject) {
                if (this.started) {
                    MessageCenter<T>.Instance.Stop();
                    MessageCenter<T>.Instance.MessageHandling -= OnMessageHandling;
                    this.started = false;
                }
            }
        }
               
        
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
        

        private void OnMessageHandling(object sender, Message<T> args)
        {
            var message = (T)args.Body;
            if (message.IsNull()) {
                Console.WriteLine("empty message.");
                return;
            }

            try {
                this.Process(message);
            }
            catch (Exception ex) {
                logger.Error("An exception happened while processing message through handler", ex);
                logger.Warn("Error will be ignored and message receiving will continue.");
            }
        }
    }
}
