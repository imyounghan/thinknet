using ThinkNet.Configurations;

namespace ThinkNet.Infrastructure
{
    public interface IMessageBroker
    {
        int Count { get; }

        void Add(Message message);

        bool TryAdd(Message message);

        bool TryTake(out Message message);

        Message Take();

        void Complete(Message message);
    }
}
