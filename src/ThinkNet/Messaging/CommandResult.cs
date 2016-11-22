using System;
using System.Collections;
using System.Runtime.Serialization;
using ThinkNet.Common;
using ThinkNet.Contracts;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 命令处理结果的回复
    /// </summary>
    [DataContract]
    [Serializable]
    public sealed class CommandResult : ICommandResult, IMessage, IUniquelyIdentifiable
    {
        /// <summary>
        /// 命令处理状态。
        /// </summary>
        [DataMember]
        public CommandStatus Status { get; set; }
        /// <summary>
        /// Represents the unique identifier of the command.
        /// </summary>
        [DataMember]
        public string CommandId { get; set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        [DataMember]
        public string ErrorMessage { get; set; }
        /// <summary>
        /// 错误编码
        /// </summary>
        [DataMember]
        public string ErrorCode { get; set; }
        /// <summary>
        /// 设置或获取一个提供用户定义的其他异常信息的键/值对的集合。
        /// </summary>
        [DataMember]
        public IDictionary ErrorData { get; set; }

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
        /// Default constructor.
        /// </summary>
        public CommandResult()
        { }

        ///// <summary>Parameterized constructor.
        ///// </summary>
        //public CommandResult(string commandId, CommandStatus status = CommandStatus.Success)
        //{
        //    this.Status = status;
        //    this.CommandId = commandId;
        //}

        ///// <summary>Parameterized constructor.
        ///// </summary>
        //public CommandResult(string commandId, CommandStatus status, string errorMessage, string errorCode)
        //{
        //    this.Status = status;
        //    this.CommandId = commandId;
        //    this.ErrorMessage = errorMessage;
        //    this.ErrorCode = errorCode;
        //}

        ///// <summary>Parameterized constructor.
        ///// </summary>
        //public CommandResult(string commandId, Exception exception, CommandStatus? status = null)
        //{
        //    if(exception == null)
        //        return;

        //    this.Status = status.HasValue ? status.Value : CommandStatus.Failed;
        //    this.ErrorMessage = exception.Message;
        //    this.ErrorData = exception.Data;

        //    var thinkNetException = exception as ThinkNetException;
        //    if(thinkNetException != null)
        //        this.ErrorCode = thinkNetException.MessageCode;
        //}

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CommandResult(string commandId, CommandReturnType commandReturnType = CommandReturnType.DomainEventHandled, CommandStatus status = CommandStatus.Success, string errorMessage = null, string errorCode = null)
            //: base(commandId, status, errorMessage, errorCode)
        {
            this.CommandReturnType = commandReturnType;
            this.ReplyTime = DateTime.UtcNow;
            this.Status = status;
            this.ErrorMessage = errorMessage;
            this.ErrorCode = errorCode;
        }
        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CommandResult(string commandId, Exception exception, CommandStatus? status = null, CommandReturnType commandReturnType = CommandReturnType.DomainEventHandled)
            //: base(commandId, exception)
        {
            this.CommandReturnType = commandReturnType;
            this.ReplyTime = DateTime.UtcNow;
            this.Status = status.HasValue ? status.Value : CommandStatus.Success;

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
        /// 输出该命令结果的字符串形式
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0}@{1}#{2},{3}", this.GetType().FullName, this.CommandId, this.CommandReturnType.ToString(), this.Status.ToString());
        }

        string IUniquelyIdentifiable.Id
        {
            get
            {
                return this.CommandId;
            }
        }

        string IMessage.GetKey()
        {
            return this.CommandId;
        }
    }
}
