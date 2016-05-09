using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;
using ThinkNet.Kernel;
using ThinkLib.Common;
using ThinkLib.Logging;


namespace ThinkNet.Messaging.Handling
{
    [RegisterComponent(typeof(IProcessor), "MessageProcessor")]
    public class MessageProcessor : DisposableObject, IInitializer, IProcessor
    {
        private readonly IMessageReceiver receiver;
        private readonly IMessageExecutor executor;
        private readonly MessageBroker broker;
        private readonly ILogger _logger;
        
        private readonly object lockObject = new object();
        private bool started = false;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageProcessor(IMessageReceiver receiver,
            IMessageExecutor executor)
        {
            this.receiver = receiver;
            this.executor = executor;
            this.broker = MessageBrokerFactory.Instance.GetOrCreate("message");
            this._logger = LogManager.GetLogger("ThinkZoo");
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

        protected virtual void Process()
        {
        }
        
        private void OnMessageReceived(object sender, EventArgs<Message> args)
        {
            var message = args.Data.Body as IMessage;
            if (message == null)
                return;

            try {
                executor.Execute(message);
            }
            catch (Exception ex) {
                _logger.Error("An exception happened while processing message through handler", ex);
                _logger.Warn("Error will be ignored and message receiving will continue.");
                //commandResultManager.NotifyCommandExecuted(command.Id, CommandStatus.Failed, ex);
                // NOTE: we catch ANY exceptions as this is for local 
                // development/debugging. The Windows Azure implementation 
                // supports retries and dead-lettering, which would 
                // be totally overkill for this alternative debug-only implementation.
                //Trace.TraceError("An exception happened while processing message through handler/s:\r\n{0}", e);
                //Trace.TraceWarning("Error will be ignored and message receiving will continue.");
            }
        }

        

        #region IInitializer 成员
        public void Initialize(IEnumerable<Type> types)
        {
            AggregateRootInnerHandlerProvider.Initialize(types);

            this.Start();
        }

        #endregion
    }
}
