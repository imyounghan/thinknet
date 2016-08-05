using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class CommandBus : AbstractBus, ICommandBus
    {
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
            KafkaClient.Instance.Push(commands);
        }
    }
}
