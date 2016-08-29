using System.Collections.Generic;
using System.Threading.Tasks;

namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的是一个消息总线
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// 发布一个消息
        /// </summary>
        Task PublishAsync(IMessage message);

        ///// <summary>
        ///// 发布一组消息
        ///// </summary>
        //Task PublishAsync(IEnumerable<IMessage> messages);

        ///// <summary>
        ///// 发布消息
        ///// </summary>
        //void Publish(IMessage message);

        /// <summary>
        /// 发布一组消息
        /// </summary>
        void Publish(IEnumerable<IMessage> messages);
    }
}
