
namespace ThinkNet.Common
{
    public class DefaultEnvelopeDelivery : IEnvelopeDelivery
    {
        public virtual void Post<T>(Envelope<T> envelope)
        {
            EnvelopeBuffer<T>.Instance.Complete(envelope);
        }
    }
}
