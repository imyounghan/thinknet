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
