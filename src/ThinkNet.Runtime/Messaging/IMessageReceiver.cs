using System;
using System.Threading.Tasks;
using ThinkLib.Common;
using ThinkLib.Scheduling;
using ThinkNet.Infrastructure;

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
        private readonly MessageBroker broker;
        private readonly object lockObject;

        public DefaultMessageReceiver()
        {
            this.lockObject = new object();
            this.worker = Worker.Create(Processing);
            this.broker = MessageBrokerFactory.Instance.GetOrCreate("message");
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
