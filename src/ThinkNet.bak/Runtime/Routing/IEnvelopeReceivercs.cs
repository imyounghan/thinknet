using System;

namespace ThinkNet.Runtime.Routing
{
    /// <summary>
    /// 表示接收者的接口
    /// </summary>
    public interface IEnvelopeReceiver
    {
        /// <summary>
        /// 收到信件后的处理方式
        /// </summary>
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
