using System;
using System.Threading.Tasks;
using ThinkNet.Infrastructure;
using ThinkLib.Common;
using ThinkLib.Scheduling;

namespace ThinkNet.Messaging
{
    [UnderlyingComponent(typeof(DefaultMessageReceiver))]
    public interface IMessageReceiver
    {
        /// <summary>
        /// Event raised whenever a message is received. Consumer of the event is responsible for disposing the message when appropriate.
        /// </summary>
        event EventHandler<EventArgs<Message>> MessageReceived;

        /// <summary>
        /// Starts the listener.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the listener.
        /// </summary>
        void Stop();
    }

    internal class DefaultMessageReceiver : IMessageReceiver
    {
        private readonly BaseWorker worker;
        //private readonly MessageBroker broker;
        //private readonly object lockObject;

        public DefaultMessageReceiver()
        {
            //this.lockObject = new object();
            var broker = MessageBrokerFactory.Instance.GetOrCreate("message");
            this.worker = new ParallelWorker<Message>(broker.Take, Processing, broker.Complete, null);
        }



        private Task Processing(Message message)
        {
            return Task.Factory
                .StartNew((state) => {
                    this.MessageReceived(state, new EventArgs<Message>(message));
                }, this, TaskCreationOptions.PreferFairness);
        }

        public event EventHandler<EventArgs<Message>> MessageReceived = (sender, args) => { };

        public void Start()
        {
            worker.Start();
        }

        public void Stop()
        {
            worker.Stop();
        }
    }
}
