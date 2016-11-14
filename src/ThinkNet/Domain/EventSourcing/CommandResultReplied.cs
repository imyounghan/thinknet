using System;
using System.Runtime.Serialization;
using ThinkNet.Contracts;
using ThinkNet.Messaging;

namespace ThinkNet.Domain.EventSourcing
{
    /// <summary>
    /// 命令处理结果的回复
    /// </summary>
    [DataContract]
    [Serializable]
    public class CommandResultReplied : CommandResult, IMessage
    {
        public CommandResultReplied()
        { }

        public CommandResultReplied(string commandId, CommandReturnType commandReturnType, CommandStatus status, string errorMessage = null, string errorCode = null)
            : base(commandId, status, errorMessage, errorCode)
        {
            this.CommandReturnType = commandReturnType;
            this.ReplyTime = DateTime.UtcNow;
        }

        public CommandResultReplied(string commandId, CommandReturnType commandReturnType, Exception exception)
            : base(commandId, exception)
        {
            this.CommandReturnType = commandReturnType;
            this.ReplyTime = DateTime.UtcNow;
        }

        [DataMember]
        public CommandReturnType CommandReturnType { get; set; }

        [DataMember]
        public DateTime ReplyTime { get; set; }

        public override string ToString()
        {
            return string.Format("CommandId:{0},CommandReturnType:{1},Status:{2},Error:{3}.",
                CommandId,
                CommandReturnType,
                Status,
                ErrorMessage.IfEmpty("N/A"));
        }

        #region IMessage 成员

        string IMessage.GetKey()
        {
            return this.CommandId;
        }

        #endregion
    }
}
