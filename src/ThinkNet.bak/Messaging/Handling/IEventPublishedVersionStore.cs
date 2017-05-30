using ThinkNet.Domain.EventSourcing;
using ThinkNet.Infrastructure;

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
        void AddOrUpdatePublishedVersion(SourceKey sourceKey, int version);
        /// <summary>
        /// 获取已发布的版本号
        /// </summary>
        int GetPublishedVersion(SourceKey sourceKey);
    }
}
