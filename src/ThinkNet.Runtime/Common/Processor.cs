using System;
using System.Collections.Generic;

namespace ThinkNet.Common
{
    public abstract class Processor : DisposableObject, IProcessor
    {
        private readonly IList<Worker> workers;
        private readonly object lockObject;
        private bool started;

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        protected Processor()
        {
            this.lockObject = new object();
            this.workers = new List<Worker>();
        }

        protected Worker BuildWorker<TMessage>(Action<TMessage> action, TMessage message)
        {
            var worker = new Worker<TMessage>(action, message, null, null);
            workers.Add(worker);

            return worker;
        }

        protected Worker BuildWorker<TMessage>(Func<TMessage> factory, Action<TMessage> action)
        {
            var worker = WorkerFactory.Create<TMessage>(action, factory);
            workers.Add(worker);

            return worker;
        }

        protected Worker BuildWorker(Action action)
        {
            var worker = WorkerFactory.Create(action);
            workers.Add(worker);
            return worker;
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
