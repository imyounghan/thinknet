using System;

namespace ThinkNet.Messaging
{
    public interface IEnvelopeReceiver
    {
        event EventHandler<Envelope> EnvelopeReceived;

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
