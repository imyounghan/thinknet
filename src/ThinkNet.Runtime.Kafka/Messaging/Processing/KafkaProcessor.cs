using ThinkNet.Infrastructure;
using ThinkNet.Messaging.Handling;

namespace ThinkNet.Messaging.Processing
{
    public class KafkaProcessor : DefaultProcessor
    {
        private readonly ITopicProvider _topicProvider;

        public KafkaProcessor(IEnvelopeReceiver receiver,
            ICommandNotification notification,
            IHandlerProvider handlerProvider,
            IHandlerRecordStore handlerStore,
            IEventBus eventBus,
            IEventPublishedVersionStore eventPublishedVersionStore,
            ISerializer serializer,
            ICommandService commandService,
            ITopicProvider topicProvider)
            : base(receiver, notification, handlerProvider, handlerStore, eventBus, eventPublishedVersionStore, serializer)
        {
            this._topicProvider = topicProvider;
            base.AddExecutor("CommandReply", new CommandReplyExecutor(commandService));
        }

        private void UpdateOffset(object sender, Envelope envelope)
        {
            var topic = _topicProvider.GetTopic(envelope.Body);

            OffsetPositionManager.Instance.Remove(topic, envelope.CorrelationId);
        }

        protected override void Subscribe(IEnvelopeReceiver receiver)
        {
            receiver.EnvelopeReceived += UpdateOffset;
        }

        protected override void Unsubscribe(IEnvelopeReceiver receiver)
        {
            receiver.EnvelopeReceived -= UpdateOffset;
        }

        protected override string GetKind(object data)
        {
            if(data is CommandReply)
                return "CommandReply";

            return base.GetKind(data);
        }
    }
}
