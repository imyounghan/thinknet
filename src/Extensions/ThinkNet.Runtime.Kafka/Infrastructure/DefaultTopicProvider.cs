using System;
using System.Collections.Generic;
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

            throw new ThinkNetException(string.Format("Unknown topic from the type of '{0}'.", payload.GetType().FullName));
        }

        public Type GetType(string topic)
        {
            switch(topic) {
                case "EventStreams":
                    return typeof(EventStream);
                case "CommandResults":
                    return typeof(CommandReply);
                case "Commands":
                case "Events":
                    return typeof(IDictionary<string, string>);
                default:
                    throw new ThinkNetException(string.Format("Unknown topic of '{0}'.", topic));
            }
        }

        #endregion
    }
}
