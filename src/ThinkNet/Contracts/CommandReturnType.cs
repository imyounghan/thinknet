
namespace ThinkNet.Contracts
{
    /// <summary>
    /// 命令处理类型
    /// </summary>
    [System.Serializable]
    public enum CommandReturnType : byte
    {
        /// <summary>Return the command result when the command execution has the following cases:
        /// 1. the command execution meets some error or exception;
        /// 2. the command execution makes nothing changes of domain;
        /// 3. the command execution success, and the domain event is sent to the message queue successfully.
        /// </summary>
        CommandExecuted,

        /// <summary>Return the command result when the command execution has the following cases:
        /// 1. the command execution meets some error or exception;
        /// 2. the command execution makes nothing changes of domain;
        /// 3. the command execution success, and the domain event is handled.
        /// </summary>
        DomainEventHandled
    }
}
