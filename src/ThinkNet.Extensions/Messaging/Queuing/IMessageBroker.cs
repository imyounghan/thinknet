
namespace ThinkNet.Messaging.Queuing
{
    public interface IMessageBroker
    {
        bool TryAdd(Message message);

        bool TryTake(out Message message);

        Message Take();

        void Complete(Message message);
    }
}
