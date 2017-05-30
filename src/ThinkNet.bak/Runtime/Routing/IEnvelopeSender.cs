
namespace ThinkNet.Runtime.Routing
{
    /// <summary>
    /// 发送信件的接口
    /// </summary>
    public interface IEnvelopeSender
    {
        /// <summary>
        /// Sends an envelope.
        /// </summary>
        void Send(Envelope envelope);
    }
}
