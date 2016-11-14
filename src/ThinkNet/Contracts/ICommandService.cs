using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示用于命令的服务
    /// </summary>
    [ServiceContract(Name = "CommandService")]
    public interface ICommandService
    {
        /// <summary>
        /// 发送命令
        /// </summary>
        [OperationContract]
        void Send(ICommand command);

        /// <summary>
        /// 异步发送命令
        /// </summary>
        [OperationContract]
        Task SendAsync(ICommand command);

        /// <summary>
        /// 在规定时间内执行一个命令
        /// </summary>
        [OperationContract]
        CommandResult Execute(ICommand command, CommandReturnType returnType, int millisecondsTimeout = -1);

        /// <summary>
        /// 在规定时间内执行一个命令
        /// </summary>
        [OperationContract]
        CommandResult Execute(ICommand command, CommandReturnType returnType, TimeSpan timeout);
        /// <summary>
        /// 异步执行一个命令
        /// </summary>
        [OperationContract]
        Task<CommandResult> ExecuteAsync(ICommand command, CommandReturnType returnType);
    }
}
