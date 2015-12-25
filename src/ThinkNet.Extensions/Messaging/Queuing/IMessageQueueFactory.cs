

namespace ThinkNet.Messaging.Queuing
{
    //[RequiredComponent(typeof(DefaultMessageQueueFactory))]
    public interface IMessageQueueFactory
    {
        /// <summary>
        /// 创建一个队列。
        /// </summary>
        IMessageQueue Create();
        /// <summary>
        /// 创建一组队列。
        /// </summary>
        IMessageQueue[] CreateGroup(int count);
    }
}
