using ThinkNet.Infrastructure;

namespace ThinkNet.Common
{
    public class EnvelopeDelivery : DefaultEnvelopeDelivery
    {
        private readonly ITopicProvider _topicProvider;

        public EnvelopeDelivery(ITopicProvider topicProvider)
        {
            this._topicProvider = topicProvider;
        }

        public override void Post<T>(Envelope<T> envelope)
        {
            base.Post<T>(envelope);

            var topic = _topicProvider.GetTopic(envelope.Body);

            OffsetPositionManager.Instance.Remove(topic, envelope.CorrelationId);
        }
    }
}
