using ThinkNet.Messaging;

namespace ThinkNet.Runtime.Routing
{
    public class DefaultRoutingKeyProvider : IRoutingKeyProvider
    {

        #region IRoutingKeyProvider 成员

        public virtual string GetRoutingKey(object payload)
        {
            //var reply = payload as RepliedCommand;
            //if(reply != null) {
            //    return reply.CommandId;
            //}

            //var command = payload as ICommand;
            //if (command != null) {
            //    return command.AggregateRootId;
            //}

            //var @event = payload as IEvent;
            //if (@event != null) {
            //    return @event.SourceId;
            //}
            var message = payload as IMessage;
            if (message != null) {
                return message.GetKey();
            }

            return null;
        }

        #endregion
    }
}
