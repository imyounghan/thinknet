using ThinkNet.Configurations;

namespace ThinkNet.Messaging.Handling
{
    public interface IMessageExecutor
    {
        /// <summary>
        /// 执行消息
        /// </summary>
        void Execute(object message);
    }
}
