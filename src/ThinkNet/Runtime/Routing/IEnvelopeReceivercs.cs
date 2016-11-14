using System;

namespace ThinkNet.Runtime.Routing
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
