namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 表示继承此接口的是一个消息处理器。
    /// </summary>
    public interface IMessageHandler<TMessage> : IHandler
        where TMessage : class, IMessage
    {
        /// <summary>
        /// 处理消息。
        /// </summary>
        void Handle(TMessage message);
    }
}
