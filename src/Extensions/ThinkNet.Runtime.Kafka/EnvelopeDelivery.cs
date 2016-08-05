using ThinkNet.Common;

namespace ThinkNet.Runtime
{
    public class EnvelopeDelivery : DefaultEnvelopeDelivery
    {
        private readonly ITopicProvider topicProvider;

        public EnvelopeDelivery(ITopicProvider topicProvider)
        {
            this.topicProvider = topicProvider;
        }

        public override void Post<T>(Envelope<T> envelope)
        {
            base.Post<T>(envelope);

            var topic = topicProvider.GetTopic(envelope.Body);
            KafkaClient.Instance.ConsumerComplete(topic, envelope.CorrelationId);
        }
    }
}
