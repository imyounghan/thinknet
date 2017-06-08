
namespace ThinkNet.Messaging
{
    using System;
    using System.Collections;

    public class CommandResult : ICommandResult
    {
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
