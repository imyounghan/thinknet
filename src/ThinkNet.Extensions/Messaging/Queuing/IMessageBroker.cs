using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging.Queuing
{
    [RequiredComponent(typeof(DefaultMessageBroker))]
    public interface IMessageBroker
    {
        bool TryAdd(Message message);

        bool TryTake(out Message message);

        Message Take();

        void Complete(Message message);
    }
}
