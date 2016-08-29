using System.Collections.Generic;
using System.Threading.Tasks;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的是命令总线
    /// </summary>
    public interface ICommandBus
    {
        /// <summary>
        /// 发送命令
        /// </summary>
        void Send(ICommand command);
        /// <summary>
        /// 发送一组命令(分布式情况下可能会延迟发送)
        /// </summary>
        void Send(IEnumerable<ICommand> commands);

        ///// <summary>
        ///// 异步发送命令
        ///// </summary>
        //Task SendAsync(ICommand command);
        ///// <summary>
        ///// 异步发送一组命令
        ///// </summary>
        //Task SendAsync(IEnumerable<ICommand> commands);
    }
}
