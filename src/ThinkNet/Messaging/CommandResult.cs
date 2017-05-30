
namespace ThinkNet.Messaging
{
    using System;
    using System.Collections;
    using ThinkNet.Contracts;

    public class CommandResult : ICommandResult
    {
        public string ProcessId { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public IDictionary ErrorData { get; set; }
        public DateTime ReplyTime { get; set; }
    }
}
