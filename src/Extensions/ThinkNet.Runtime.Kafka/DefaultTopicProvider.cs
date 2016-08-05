using System;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class DefaultTopicProvider : ITopicProvider
    {
        #region ITopicProvider 成员

        public string GetTopic(object payload)
        {
            if (payload is ICommand) {
                return "Commands";
            }
            
            if (payload is IEvent) {
                return "Events";
            }

            if (payload is CommandReply) {
                return "CommandResults";
            }

            throw new Exception();
        }

        #endregion
    }
}
