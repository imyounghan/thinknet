using System;
using System.Threading.Tasks;
using ThinkLib.Scheduling;
using ThinkNet.Common;
using ThinkNet.Messaging.Queuing;

namespace ThinkNet.Messaging
{
    [RequiredComponent(typeof(DefaultMessageReceiver))]
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
        private readonly Worker worker;
        private readonly IMessageBroker broker;
        private readonly object lockObject;

        public DefaultMessageReceiver(IMessageBroker messageBroker)
        {
            this.lockObject = new object();
            this.worker = Worker.Create("", Processing);
            this.broker = messageBroker;
        }

        private void Processing()
        {
            var message = broker.Take();
            if(message != null) {
                Task.Factory
                    .StartNew((state) => {
                        this.MessageReceived(state, new EventArgs<Message>(message));
                        return message;
                    }, this, TaskCreationOptions.PreferFairness)
                    .ContinueWith(task => {
                        broker.Complete(task.Result);
                    }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.PreferFairness);
            }
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
