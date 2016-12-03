using ThinkNet.Domain.EventSourcing;

namespace ThinkNet.Messaging.Handling
{
    /// <summary>
    /// 用于存储已发布事件的版本号的接口
    /// </summary>
    public interface IEventPublishedVersionStore
    {
        /// <summary>
        /// 更新版本号
        /// </summary>
        void AddOrUpdatePublishedVersion(DataKey sourceKey, int version);
        /// <summary>
        /// 获取已发布的版本号
        /// </summary>
        int GetPublishedVersion(DataKey sourceKey);
    }
}
