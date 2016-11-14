
namespace ThinkNet.Messaging
{
    /// <summary>
    /// 表示继承该接口的是事件总线
    /// </summary>
    public interface IMessageBus
    {
        /// <summary>
        /// 发布消息
        /// </summary>
        void Publish(IMessage message);

        /// <summary>
        /// 发布一组消息
        /// </summary>
        void Publish(System.Collections.Generic.IEnumerable<IMessage> messages);
    }   
}
