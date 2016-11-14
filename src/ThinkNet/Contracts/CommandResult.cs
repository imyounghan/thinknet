using System;
using System.Collections;
using System.Runtime.Serialization;


namespace ThinkNet.Contracts
{
    /// <summary>
    /// 命令处理结果
    /// </summary>
    [DataContract]
    [Serializable]
    public class CommandResult : IDataTransferObject
    {
        /// <summary>
        /// 命令处理状态。
        /// </summary>
        [DataMember]
        public CommandStatus Status { get; private set; }
        /// <summary>
        /// Represents the unique identifier of the command.
        /// </summary>
        [DataMember]
        public string CommandId { get; private set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        [DataMember]
        public string ErrorMessage { get; private set; }
        /// <summary>
        /// 错误编码
        /// </summary>
        [DataMember]
        public string ErrorCode { get; private set; }
        /// <summary>
        /// 设置或获取一个提供用户定义的其他异常信息的键/值对的集合。
        /// </summary>
        [DataMember]
        public IDictionary ErrorData { get; private set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected CommandResult()
        {
            this.Status = CommandStatus.Success;
        }

        /// <summary>Parameterized constructor.
        /// </summary>
        public CommandResult(string commandId, CommandStatus status = CommandStatus.Success)
        {
            this.Status = status;
            this.CommandId = commandId;
        }

        /// <summary>Parameterized constructor.
        /// </summary>
        public CommandResult(string commandId, CommandStatus status, string errorMessage, string errorCode)
        {
            this.Status = status;
            this.CommandId = commandId;
            this.ErrorMessage = errorMessage;
            this.ErrorCode = errorCode;
        }

        /// <summary>Parameterized constructor.
        /// </summary>
        public CommandResult(string commandId, Exception exception, CommandStatus? status = null)
            : this(commandId)
        {
            if(exception == null)
                return;

            this.Status = status.HasValue ? status.Value : CommandStatus.Failed;
            this.ErrorMessage = exception.Message;            
            this.ErrorData = exception.Data;

            var thinkNetException = exception as ThinkNetException;
            if(thinkNetException != null)
                this.ErrorCode = thinkNetException.MessageCode;
        }
        

        /// <summary>
        /// Overrides to return the command result info.
        /// </summary>
        public override string ToString()
        {
            return string.Format("[CommandId={0},Status={1},ErrorMessage={3}]",
                CommandId,
                Status,
                ErrorMessage);
        }

        /// <summary>
        /// 空的结果
        /// </summary>
        public static readonly CommandResult Empty = new CommandResult();
    }
}
