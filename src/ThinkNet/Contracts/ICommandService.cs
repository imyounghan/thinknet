
namespace ThinkNet.Contracts
{
    public interface ICommandService
    {
        /// <summary>
        /// 发送一个命令
        /// </summary>      
        ICommandResult Send(ICommand command);


        /// <summary>
        /// 在规定时间内执行一个命令，如果为0表示无限等待直到服务器返回结果
        /// </summary>
        ICommandResult Execute(ICommand command, int mill);
    }
}
