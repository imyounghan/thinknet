using ThinkNet.Messaging;

namespace ThinkNet.Runtime.Routing
{
    /// <summary>
    /// <see cref="IRoutingKeyProvider"/> 的默认实现
    /// </summary>
    public class DefaultRoutingKeyProvider : IRoutingKeyProvider
    {

        #region IRoutingKeyProvider 成员
        /// <summary>
        /// 获取路由的Key
        /// </summary>
        public virtual string GetRoutingKey(object payload)
        {
            var reply = payload as CommandResult;
            if(reply != null) {
                return reply.CommandId;
            }

            var eventCollection = payload as EventCollection;
            if(eventCollection != null) {
                return eventCollection.SourceId.Id;
            }

            var command = payload as Command;
            if(command != null) {
                return command.GetKey();
            }

            var @event = payload as Event;
            if(@event != null) {
                return @event.GetKey();
            }
            

            return null;
        }

        #endregion
    }
}
