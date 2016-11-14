using System.Collections.Generic;
using System.Threading.Tasks;

namespace ThinkNet.Runtime.Routing
{
    public interface IEnvelopeSender
    {
        Task SendAsync(Envelope envelope);

        /// <summary>
        /// Sends a batch of envelopes.
        /// </summary>
        Task SendAsync(IEnumerable<Envelope> envelopes);
    }
}
