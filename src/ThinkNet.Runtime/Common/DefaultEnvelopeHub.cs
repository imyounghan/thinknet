using ThinkNet.Messaging;

namespace ThinkNet.Common
{
    public class DefaultEnvelopeHub : IEnvelopeHub
    {
        #region IEnvelopeHub 成员

        public virtual void Distribute(object message)
        {
            var stream  = message as EventStream;
            if (stream != null) {
                var envelope = Transform(stream);
                EnvelopeBuffer<EventStream>.Instance.Enqueue(envelope);
                return;
            }

            var @event  = message as IEvent;
            if (@event != null) {
                var envelope = Transform(@event);
                EnvelopeBuffer<IEvent>.Instance.Enqueue(envelope);
                return;
            }

            var command  = message as ICommand;
            if (command != null) {
                var envelope = Transform(command);
                EnvelopeBuffer<ICommand>.Instance.Enqueue(envelope);
                return;
            }
        }

        #endregion

        protected Envelope<T> Transform<T>(T message)
            where T : IMessage
        {
            return new Envelope<T>(message) {
                CorrelationId = message.Id
            };
        }
    }
}
