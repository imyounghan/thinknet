namespace ThinkNet.Messaging.Processing
{
    public interface IMessageExecutor
    {
        /// <summary>
        /// 执行消息结果。
        /// </summary>
        bool Execute(IMessage message, out System.TimeSpan processTime);
    }
}
