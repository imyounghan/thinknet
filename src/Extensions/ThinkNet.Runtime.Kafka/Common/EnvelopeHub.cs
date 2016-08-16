using ThinkNet.Messaging;

namespace ThinkNet.Common
{
    public class EnvelopeHub : DefaultEnvelopeHub
    {
        public override void Distribute(object message)
        {
            var notification = message as CommandReply;
            if (notification != null) {
                var envelope = Transform(notification);
                EnvelopeBuffer<CommandReply>.Instance.Enqueue(envelope);
                return;
            }

            base.Distribute(message);
        }
    }
}
