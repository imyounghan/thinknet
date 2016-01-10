using ThinkNet.Messaging;

namespace ThinkNet.Infrastructure
{
    public interface IMessageExecutor
    {
        /// <summary>
        /// 执行消息
        /// </summary>
        void Execute(IMessage message);
    }
}
