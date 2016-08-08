using ThinkNet.Common;

namespace ThinkNet.Runtime
{
    public class EnvelopeDelivery : DefaultEnvelopeDelivery
    {
        private readonly ITopicProvider _topicProvider;
        private readonly KafkaClient _kafkaClient;

        public EnvelopeDelivery(ITopicProvider topicProvider, KafkaClient kafkaClient)
        {
            this._topicProvider = topicProvider;
            this._kafkaClient = kafkaClient;
        }

        public override void Post<T>(Envelope<T> envelope)
        {
            base.Post<T>(envelope);

            var topic = _topicProvider.GetTopic(envelope.Body);
            _kafkaClient.ConsumerComplete(topic, envelope.CorrelationId);
        }
    }
}
