using System;
using System.Runtime.Serialization;

namespace ThinkNet.Messaging
{
    [DataContract]
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

        [DataMember]
        public CommandResultType CommandResultType { get; set; }

        #region IMessage 成员
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public DateTime CreatedTime { get; set; }
        #endregion
    }
}
