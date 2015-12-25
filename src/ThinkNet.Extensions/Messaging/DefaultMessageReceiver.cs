using System;
using ThinkLib.Scheduling;
using ThinkNet.Messaging.Queuing;

namespace ThinkNet.Messaging
{
    public class DefaultMessageReceiver : IMessageReceiver
    {
        private readonly Worker worker;
        private readonly IMessageBroker broker;


        private void Processing()
        {
            var message = broker.Take();
            if (message != null) {
            }
        }

        public event EventHandler<EventArgs<MetaMessage>> MessageReceived = (sender, args) => { };

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
