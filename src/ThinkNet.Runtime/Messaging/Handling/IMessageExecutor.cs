using ThinkLib.Common;

namespace ThinkNet.Messaging.Handling
{
    [UnderlyingComponent(typeof(MessageExecutor))]
    public interface IMessageExecutor
    {
        /// <summary>
        /// 执行消息
        /// </summary>
        void Execute(IMessage message);
    }
}
