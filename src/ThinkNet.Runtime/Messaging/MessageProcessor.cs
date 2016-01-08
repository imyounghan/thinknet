using System;
using System.Threading;
using ThinkNet.EventSourcing;
using ThinkNet.Infrastructure;
using ThinkNet.Kernel;
using ThinkNet.Messaging.Queuing;


namespace ThinkNet.Messaging
{
    public class MessageProcessor : DisposableObject, IProcessor
    {
        private readonly IMessageReceiver receiver;
        private readonly IMessageExecutor executor;
        private readonly IEventPublishedVersionStore eventPublishedVersionStore;
        private readonly IMessageBroker broker;
        
        private readonly object lockObject = new object();
        private bool disposed;
        private bool started = false;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public MessageProcessor()
        {
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
            if (!this.disposed) {
                if (disposing) {
                    this.Stop();
                    this.disposed = true;

                    using (this.receiver as IDisposable) {
                        // Dispose receiver if it's disposable.
                    }
                }
            }
        }

        /// <summary>
        /// 检查是否为有序的消息
        /// </summary>
        protected virtual bool CheckOrderly(Message message)
        {
            var stream = message.Body as VersionedEventStream;
            if (stream != null) {
                var version = eventPublishedVersionStore.GetPublishedVersion(stream.AggregateRoot.SourceTypeName, stream.AggregateRoot.SourceId);

                return version + 1 == stream.StartVersion;
            }

            return true;
        }

        protected virtual void ProcessMessage(Message message)
        {
            var msg = message.Body as IMessage;

            int count = 0;
            int retryTimes = 1;

            while (count++ < retryTimes) {
                try {
                    executor.Execute(msg);
                    break;
                }
                catch (Exception) {
                    if (count == retryTimes)
                        throw;
                    else
                        Thread.Sleep(1000);
                }
            }
        }

        private void OnMessageReceived(object sender, EventArgs<Message> args)
        {           

            try {
                if (CheckOrderly(args.Data)) { //如果是有序的消息
                    ProcessMessage(args.Data);
                }
                else { //否则重新进队列等待正确的消息
                    broker.TryAdd(args.Data);
                }
            }
            catch (Exception e) {
                // NOTE: we catch ANY exceptions as this is for local 
                // development/debugging. The Windows Azure implementation 
                // supports retries and dead-lettering, which would 
                // be totally overkill for this alternative debug-only implementation.
                //Trace.TraceError("An exception happened while processing message through handler/s:\r\n{0}", e);
                //Trace.TraceWarning("Error will be ignored and message receiving will continue.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
                throw new ObjectDisposedException("MessageProcessor");
        }


    }
}
