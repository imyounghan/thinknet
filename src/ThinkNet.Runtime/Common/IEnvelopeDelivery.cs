
namespace ThinkNet.Common
{
    public interface IEnvelopeDelivery
    {
        void Post<T>(Envelope<T> envelope);
    }
}
