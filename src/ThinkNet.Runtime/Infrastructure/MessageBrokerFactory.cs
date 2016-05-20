using System.Collections.Concurrent;

namespace ThinkNet.Infrastructure
{
    public class MessageBrokerFactory
    {
        public readonly static MessageBrokerFactory Instance = new MessageBrokerFactory();

        private readonly ConcurrentDictionary<string, MessageBroker> _brokers;
        private MessageBrokerFactory()
        {
            this._brokers = new ConcurrentDictionary<string, MessageBroker>();
        }

        public MessageBroker this[string catalog]
        {
            get { return _brokers.GetOrAdd(catalog, _ => new MessageBroker()); }
        }

        public MessageBroker GetOrCreate(string catalog)
        {
            return _brokers.GetOrAdd(catalog, _ => new MessageBroker());
        }
    }
}
