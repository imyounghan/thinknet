using System;
using System.Threading.Tasks;
using ThinkLib.Scheduling;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    internal class DefaultMessageReceiver : IMessageReceiver
    {
        private readonly BaseWorker worker;

        public DefaultMessageReceiver(IMessageBroker broker)
        {
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
