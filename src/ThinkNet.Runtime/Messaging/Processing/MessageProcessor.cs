using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ThinkNet.Common;
using ThinkNet.Configurations;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;


namespace ThinkNet.Messaging.Processing
{
    public abstract class MessageProcessor<T> : DisposableObject, IProcessor, IInitializer
        where T : IMessage
    {
        //private readonly IHandlerRecordStore _handlerStore;
        ///// <summary>
        ///// Parameterized Constructor.
        ///// </summary>
        //public MessageProcessor(IHandlerRecordStore handlerStore)
        //{
        //    this._handlerStore = handlerStore;
        //}


        //protected void DuplicateProcessHandler(IProxyHandler handler, IMessage message, Type messageType)
        //{
        //    try {
        //        handler.Handle(message);


        //        if (LogManager.Default.IsDebugEnabled) {
        //            var debugMessage = string.Format("Handle message success. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
        //                handler.HanderType.FullName, messageType.FullName, message.Id);
        //            LogManager.Default.DebugFormat(debugMessage);
        //        }
        //    }
        //    catch (Exception) {
        //        if (LogManager.Default.IsErrorEnabled) {
        //            var errorMessage = string.Format("Exception raised when {0} handling {1}. message info:{2}.",
        //                handler.HanderType.FullName, messageType.FullName, message.Id);
        //            LogManager.Default.Error(errorMessage);
        //        }
        //        throw;
        //    }
        //}

        //protected void OnlyonceProcessHandler(IProxyHandler handler, IMessage message, Type messageType)
        //{
        //    try {
        //        if (_handlerStore.HandlerIsExecuted(message.Id, messageType, handler.HanderType)) {
        //            if (LogManager.Default.IsDebugEnabled)
        //                LogManager.Default.DebugFormat("The message has been handled. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
        //                     handler.HanderType.FullName, messageType.FullName, message.Id);
        //            return;
        //        }
        //    }
        //    catch (Exception ex) {
        //        var errorMsg = string.Format("Check the handler info raised exception. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
        //            handler.HanderType.FullName, messageType.FullName, message.Id);
        //        throw new HandlerRecordStoreException(errorMsg, ex);
        //    }

        //    DuplicateProcessHandler(handler, message, messageType);

        //    try {
        //        _handlerStore.AddHandlerInfo(message.Id, messageType, handler.HanderType);
        //    }
        //    catch (Exception ex) {
        //        var errorMsg = string.Format("Save the handler info raised exception. messageHandlerType:{0}, messageType:{1}, messageId:{2}",
        //            handler.HanderType.FullName, messageType.FullName, message.Id);
        //        throw new HandlerRecordStoreException(errorMsg, ex);
        //    }
        //}

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

            this.Notify(item.Body, exception);
            EnvelopeBuffer<T>.Instance.Complete(item);
            if (exception != null) {
                if (LogManager.Default.IsErrorEnabled) {
                    var errorMessage = string.Format("Exception raised when handling {1}. message info:{2}.",
                        item.Body.GetType().FullName, item.Body);
                    LogManager.Default.Error(errorMessage);
                }
            }
        }

        #region IInitializer 成员
        public void Initialize(IEnumerable<Type> types)
        {
            this.Start();
        }

        #endregion


        private readonly BlockingCollection<Envelope<T>>[] brokers;
        private readonly IList<Worker> workers;
        
        private readonly object lockObject;
        private bool started;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected MessageProcessor()
        {
            this.lockObject = new object();

            var count = ConfigurationSetting.Current.ProcessorCount;
            this.brokers = new BlockingCollection<Envelope<T>>[count];
            this.workers = new List<Worker>();

            this.BuildWorker<Envelope<T>>(EnvelopeBuffer<T>.Instance.Dequeue, Distribute);
            for (int i = 0; i < count; i++) {
                brokers[i] = new BlockingCollection<Envelope<T>>();
                this.BuildWorker<Envelope<T>>(brokers[i].Take, Process);
            }
        }

        protected void BuildWorker<TMessage>(Func<TMessage> factory, Action<TMessage> action)
        {
            var worker = WorkerFactory.Create<TMessage>(factory, action);
            workers.Add(worker);
        }

        protected void BuildWorker(Action action)
        {
            var worker = WorkerFactory.Create(action);
            workers.Add(worker);
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

        /// <summary>
        /// Starts the listener.
        /// </summary>
        public virtual void Start()
        {
            ThrowIfDisposed();
            lock (this.lockObject) {
                if (!this.started) {
                    workers.ForEach(worker => worker.Start());
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
                    workers.ForEach(worker => worker.Stop());
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
    }
}
