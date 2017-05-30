using System.Collections.Generic;
using System.Threading.Tasks;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的是事件总线
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// 异步发布消息
        /// </summary>
        Task PublishAsync(IMessage message);

        /// <summary>
        /// 异步发布一组消息
        /// </summary>
        Task PublishAsync(IEnumerable<IMessage> messages);
    }   
}
