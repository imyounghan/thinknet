using System;
using System.Runtime.Serialization;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 命令处理结果的回复
    /// </summary>
    [DataContract]
    [Serializable]
    public sealed class CommandResultReplied : CommandResult, IMessage
    {
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public CommandResultReplied()
        { }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CommandResultReplied(string commandId, CommandReturnType commandReturnType, CommandStatus status, string errorMessage = null, string errorCode = null)
            : base(commandId, status, errorMessage, errorCode)
        {
            this.CommandReturnType = commandReturnType;
            this.ReplyTime = DateTime.UtcNow;
        }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CommandResultReplied(string commandId, CommandReturnType commandReturnType, Exception exception)
            : base(commandId, exception)
        {
            this.CommandReturnType = commandReturnType;
            this.ReplyTime = DateTime.UtcNow;
        }
        /// <summary>
        /// 返回类型
        /// </summary>
        [DataMember]
        public CommandReturnType CommandReturnType { get; set; }
        /// <summary>
        /// 回复时间
        /// </summary>
        [DataMember]
        public DateTime ReplyTime { get; set; }

        /// <summary>
        /// 输出该命令结果的字符串形式
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}@{1}#{2},{3}", this.GetType().FullName, this.CommandId, this.CommandReturnType.ToString(), this.Status.ToString());
        }

        #region IMessage 成员

        string IMessage.GetKey()
        {
            return this.CommandId;
        }

        #endregion
    }
}
