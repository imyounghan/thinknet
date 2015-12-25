using ThinkNet.Infrastructure;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承此接口的是一个命令处理器。
    /// </summary>
    public interface ICommandHandler<in TCommand> : IHandler
        where TCommand : class, ICommand
    {
        /// <summary>
        /// 处理命令。
        /// </summary>
        void Handle(ICommandContext commandContext, TCommand command);
    }
}
