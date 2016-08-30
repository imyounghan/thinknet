using System;
using System.Runtime.Serialization;
using ThinkNet.Infrastructure;

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
            this.CommandReturnType = CommandReturnType.DomainEventHandled;
            this.Init();
        }

        public CommandReply(string commandId, Exception exception, CommandReturnType commandReturnType)
            : base(commandId, exception)
        {
            this.CommandReturnType = commandReturnType;
            this.Init();
        }

        private void Init()
        {
            this.Id = ObjectId.GenerateNewStringId();
            this.CreatedTime = DateTime.UtcNow;
        }

        [DataMember]
        public CommandReturnType CommandReturnType { get; set; }

        #region IMessage 成员
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public DateTime CreatedTime { get; set; }
        #endregion
    }
}
