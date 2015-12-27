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
        private readonly ICommandResultManager _commandResultManager;

        protected override bool SearchMatchType(Type type)
        {
            return TypeHelper.IsCommand(type);
        }
        public Task<CommandResult> SendAsync(ICommand command, CommandReplyType commandReplyType)
        {
            var task = _commandResultManager.RegisterCommand(command, commandReplyType);

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


        private static MetaMessage Map(ICommand command)
        {
            return new MetaMessage {
                Body = command,
                Topic = "Command",
                RoutingKey = command.GetRoutingKey()
            };
        }
    }
}
