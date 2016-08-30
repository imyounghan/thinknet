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
            ICommandService commandService)
            : base(receiver, notification, handlerProvider, handlerStore, eventBus, eventPublishedVersionStore, serializer)
        {
            base.AddExecutor("CommandReply", new CommandReplyExecutor(commandService));
        }

        private void UpdateOffset(object sender, Envelope envelope)
        {
            var topic = _topicProvider.GetTopic(envelope.Body);

            OffsetPositionManager.Instance.Remove(topic, envelope.CorrelationId);
        }

        protected override string GetKind(object data)
        {
            if(data is CommandReply)
                return "CommandReply";

            return base.GetKind(data);
        }
    }
}
