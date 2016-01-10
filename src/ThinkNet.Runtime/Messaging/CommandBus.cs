using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    public class CommandBus : AbstractBus, ICommandBus
    {
        private readonly IMessageSender messageSender;
        private readonly ICommandResultManager commandResultManager;
        private readonly IRoutingKeyProvider routingKeyProvider;
        private readonly IMetadataProvider metadataProvider;

        public CommandBus(IMessageSender messageSender,
            ICommandResultManager commandResultManager,
            IRoutingKeyProvider routingKeyProvider,
            IMetadataProvider metadataProvider)
        {
            this.messageSender = messageSender;
            this.commandResultManager = commandResultManager;
            this.routingKeyProvider = routingKeyProvider;
            this.metadataProvider = metadataProvider;
        }

        protected override bool SearchMatchType(Type type)
        {
            return TypeHelper.IsCommand(type);
        }
        public Task<CommandResult> SendAsync(ICommand command, CommandReplyType commandReplyType)
        {
            var task = commandResultManager.RegisterCommand(command, commandReplyType);

            this.Send(command);

            return task;
        }

        public void Send(ICommand command)
        {
            this.Send(new[] { command });
        }

        public void Send(IEnumerable<ICommand> commands)
        {
            var messages = commands.Select(Map).AsEnumerable();
            messageSender.Send(messages);
        }


        private Message Map(ICommand command)
        {
            return new Message {
                Body = command,
                MetadataInfo = metadataProvider.GetMetadata(command),
                RoutingKey = routingKeyProvider.GetRoutingKey(command),
                CreatedTime = DateTime.UtcNow
            };
        }
    }
}
