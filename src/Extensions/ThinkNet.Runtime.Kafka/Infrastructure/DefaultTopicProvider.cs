using ThinkNet.Messaging;

namespace ThinkNet.Infrastructure
{
    public class DefaultTopicProvider : ITopicProvider
    {
        #region ITopicProvider 成员

        public string GetTopic(object payload)
        {
            if (payload is EventStream) {
                return "EventStreams";
            }

            if (payload is CommandReply) {
                return "CommandResults";
            }

            if (payload is ICommand) {
                return "Commands";
            }

            if (payload is IEvent) {
                return "Events";
            }            

            throw new ThinkNetException(string.Format("Can't find the type '{0}' of topic.", payload.GetType().FullName));
        }

        #endregion
    }
}
