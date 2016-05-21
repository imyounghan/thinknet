using System;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Handling
{
    public abstract class Processor : DisposableObject, IProcessor
    {
        private readonly IMessageReceiver receiver;
        private readonly ILogger logger;
        
        private readonly object lockObject = new object();
        private bool started = false;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public Processor(IMessageReceiver receiver)
        {
            this.receiver = receiver;
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
                    this.receiver.MessageReceived += OnMessageReceived;
                    this.receiver.Start();
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
                    this.receiver.Stop();
                    this.receiver.MessageReceived -= OnMessageReceived;
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

                using (this.receiver as IDisposable) {
                    // Dispose receiver if it's disposable.
                }
            }
        }

        protected abstract void Process(IMessage message);
        
        private void OnMessageReceived(object sender, EventArgs<Message> args)
        {
            var message = args.Data.Body as IMessage;
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
