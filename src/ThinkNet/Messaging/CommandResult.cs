
namespace ThinkNet.Messaging
{
    using System;
    using System.Collections;

    public class CommandResult : ICommandResult
    {
        public CommandResult()
        {
            this.ReplyTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CommandResult(string traceId)
            : this(traceId, ExecutionStatus.Success)
        {
        }

        /// <summary>
        /// Parameterized Constructor.
        /// </summary>
        public CommandResult(string traceId, ExecutionStatus status, string errorMessage = null, string errorCode = "-1")
            : this()
        {
            this.TraceId = traceId;
            this.Status = status;
            this.ErrorMessage = errorMessage;
            this.ErrorCode = errorCode;
        }

        /// <summary>
        /// 跟踪ID
        /// </summary>
        public string TraceId { get; set; }
        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// 错误编码
        /// </summary>
        public string ErrorCode { get; set; }
        public IDictionary ErrorData { get; set; }
        public DateTime ReplyTime { get; set; }
        public string ReplyServer { get; set; }
        public ExecutionStatus Status { get; set; }
    }
}
