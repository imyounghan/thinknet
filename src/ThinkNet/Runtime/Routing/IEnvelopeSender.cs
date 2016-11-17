using System.Collections.Generic;
using System.Threading.Tasks;

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
        Task SendAsync(Envelope envelope);

        /// <summary>
        /// Sends a batch of envelopes.
        /// </summary>
        Task SendAsync(IEnumerable<Envelope> envelopes);
    }
}
