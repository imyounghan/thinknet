using System;
using System.Threading.Tasks;
using ThinkLib.Scheduling;
using ThinkNet.Messaging.Queuing;

namespace ThinkNet.Messaging
{
    public interface IMessageReceiver
    {
        /// <summary>
        /// Event raised whenever a message is received. Consumer of the event is responsible for disposing the message when appropriate.
        /// </summary>
        event EventHandler<EventArgs<MetaMessage>> MessageReceived;

        /// <summary>
        /// Starts the listener.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the listener.
        /// </summary>
        void Stop();
    }

    public class DefaultMessageReceiver : IMessageReceiver
    {
        private readonly Worker worker;
        private readonly IMessageBroker broker;
        private readonly object lockObject;

        public DefaultMessageReceiver()
        {
            this.lockObject = new object();
            this.worker = Worker.Create("", Processing);
        }

        private void Processing()
        {
            var message = broker.Take();
            if(message != null) {
                //broker.
                Task.Factory
                    .StartNew((state) => {
                        this.MessageReceived(state, new EventArgs<MetaMessage>(message));
                        return message;
                    }, this, TaskCreationOptions.PreferFairness)
                    .ContinueWith(task => {
                    });
            }
        }

        public event EventHandler<EventArgs<MetaMessage>> MessageReceived = (sender, args) => { };

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
