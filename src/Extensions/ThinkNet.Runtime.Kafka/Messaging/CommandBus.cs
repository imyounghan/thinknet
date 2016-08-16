using System;
using System.Collections.Generic;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class CommandBus : KafkaBus, ICommandBus
    {
        public CommandBus(ISerializer serializer, IMetadataProvider metadataProvider, ITopicProvider topicProvider)
            : base(serializer, metadataProvider, topicProvider)
        { }


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
            if (commands.IsEmpty())
                return;

            base.Push(commands);
        }
    }
}
