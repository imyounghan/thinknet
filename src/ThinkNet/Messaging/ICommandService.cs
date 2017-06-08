
namespace ThinkNet.Messaging
{

    public interface ICommandService
    {
        /// <summary>
        /// 发送命令
        /// </summary>      
        ICommandResult Send(ICommand command);


        /// <summary>
        /// 在规定时间内执行一个命令
        /// </summary>
        ICommandResult Execute(ICommand command);
    }
}
