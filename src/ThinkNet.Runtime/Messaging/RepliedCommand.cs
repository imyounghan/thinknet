using System;
using System.Runtime.Serialization;
using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    [DataContract]
    [Serializable]
    public class RepliedCommand : CommandResult, IMessage
    {
        public RepliedCommand()
        { }

        public RepliedCommand(string commandId)
            : base(CommandStatus.NothingChanged, commandId)
        {
            this.CommandReturnType = CommandReturnType.DomainEventHandled;
            this.CreatedTime = DateTime.UtcNow;
            this.Id = ObjectId.GenerateNewStringId();
        }

        public RepliedCommand(string commandId, Exception exception, CommandReturnType commandReturnType)
            : base(commandId, exception)
        {
            this.CommandReturnType = commandReturnType;
            this.CreatedTime = DateTime.UtcNow;
            this.Id = ObjectId.GenerateNewStringId();
        }

        [DataMember]
        public CommandReturnType CommandReturnType { get; set; }

        [DataMember]
        public DateTime CreatedTime { get; set; }

        [DataMember]
        public string Id { get; set; }

        public override string ToString()
        {
            return string.Format("CommandId:{0},CommandReturnType:{1},Status:{2},Error:{3}.",
                CommandId,
                CommandReturnType,
                Status,
                ErrorMessage.IfEmpty("N/A"));
        }
    }
}
