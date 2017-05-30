

namespace ThinkNet.Messaging
{
    using System;

    public interface IMessageReceiver<TMessage>// where TMessage : IMessage
    {
        event EventHandler<TMessage> MessageReceived;


        /// <summary>
        /// Starts the listener.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the listener.
        /// </summary>
        void Stop();
    }
}
