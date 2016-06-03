using System;
using System.Collections.Generic;
using System.Linq;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    internal class DefaultCommandBus : AbstractBus, ICommandBus
    {
        private readonly IRoutingKeyProvider routingKeyProvider;
        public DefaultCommandBus(IRoutingKeyProvider routingKeyProvider)
        {
            this.routingKeyProvider = routingKeyProvider;
        }

        protected override bool MatchType(Type type)
        {
            return TypeHelper.IsCommand(type);
        }

        public void Send(ICommand command)
        {
            this.Send(new[] { command });
        }

        public void Send(IEnumerable<ICommand> commands)
        {
            commands.Select(Serialize).ForEach(MessageCenter<ICommand>.Instance.Add);
        }

        private Message<ICommand> Serialize(ICommand command)
        {
            return new Message<ICommand> {
                Body = command,
                RoutingKey = routingKeyProvider.GetRoutingKey(command)
            };
        }
    }
}
