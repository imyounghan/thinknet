
using ThinkNet.Common;
namespace ThinkNet.Messaging.Handling
{
    [RequiredComponent(typeof(MessageExecutor))]
    public interface IMessageExecutor
    {
        /// <summary>
        /// 执行消息
        /// </summary>
        void Execute(IMessage message);
    }
}
