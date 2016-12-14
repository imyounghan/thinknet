using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace ThinkNet.Contracts
{
    /// <summary>
    /// 表示用于命令的服务
    /// </summary>    
    public interface ICommandService
    {
        /// <summary>
        /// 发送命令
        /// </summary>      
        void Send(ICommand command);

        /// <summary>
        /// 异步发送命令
        /// </summary>
        //[OperationContract]
        Task SendAsync(ICommand command);

        /// <summary>
        /// 在规定时间内执行一个命令
        /// </summary>
        ICommandResult Execute(ICommand command, CommandReturnMode returnMode);
        /// <summary>
        /// 异步执行一个命令
        /// </summary>
        //[OperationContract]
        Task<ICommandResult> ExecuteAsync(ICommand command, CommandReturnMode returnMode);
    }
}
