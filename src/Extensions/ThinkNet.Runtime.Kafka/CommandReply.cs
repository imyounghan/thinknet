using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    public class CommandReply : CommandResult, IMessage
    {
        public CommandReply(string commandId)
            : base(CommandStatus.NothingChanged, commandId)
        {
            this.CommandResultType = CommandResultType.DomainEventHandled;
        }

        public CommandReply(string commandId, Exception exception, CommandResultType commandResultType)
            : base(commandId, exception)
        {
            this.CommandResultType = commandResultType;
        }

        public CommandResultType CommandResultType { get; set; }

        #region IMessage 成员

        public string Id { get { return this.CommandId; } }

        public DateTime CreatedTime { get; set; }

        #endregion
    }
}
