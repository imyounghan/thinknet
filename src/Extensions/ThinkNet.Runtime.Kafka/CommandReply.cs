using System;
using ThinkNet.Messaging;

namespace ThinkNet.Runtime
{
    [Serializable]
    public class CommandReply : CommandResult, IMessage
    {
        public CommandReply()
        { }

        public CommandReply(string commandId)
            : base(CommandStatus.NothingChanged, commandId)
        {
            this.CommandResultType = CommandResultType.DomainEventHandled;
            this.Init();
        }

        public CommandReply(string commandId, Exception exception, CommandResultType commandResultType)
            : base(commandId, exception)
        {
            this.CommandResultType = commandResultType;
            this.Init();
        }

        private void Init()
        {
            this.Id = ObjectId.GenerateNewStringId();
            this.CreatedTime = DateTime.UtcNow;
        }

        public CommandResultType CommandResultType { get; set; }

        #region IMessage 成员

        public string Id { get; set; }

        public DateTime CreatedTime { get; set; }

        #endregion
    }
}
